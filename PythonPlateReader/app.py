from collections import Counter, defaultdict
from typing import Annotated, Any

import cv2
import easyocr
import numpy as np
from core import (
    ALLOWLIST,
    UploadTooLargeError,
    bounded_int_from_env,
    clean_text,
    correct_by_plate_zones,
    is_loopback_host,
    is_plate,
    is_supported_content_type,
    read_limited_upload,
    truthy_env,
)
from fastapi import FastAPI, File, Request, UploadFile
from fastapi.responses import JSONResponse

MAX_UPLOAD_BYTES = bounded_int_from_env(
    "PLATE_READER_MAX_UPLOAD_BYTES",
    default=5 * 1024 * 1024,
    minimum=64 * 1024,
    maximum=20 * 1024 * 1024,
)
MAX_IMAGE_PIXELS = bounded_int_from_env(
    "PLATE_READER_MAX_IMAGE_PIXELS",
    default=16_000_000,
    minimum=1_000_000,
    maximum=40_000_000,
)
ALLOW_REMOTE = truthy_env("PLATE_READER_ALLOW_REMOTE")

app = FastAPI(
    title="Python Plate Reader",
    version="1.1.0",
    description="Local-only OCR companion service for the parking desktop application.",
)
reader: easyocr.Reader | None = None


@app.middleware("http")
async def local_only(request: Request, call_next):
    client_host = request.client.host if request.client else None
    if not ALLOW_REMOTE and not is_loopback_host(client_host):
        return JSONResponse(
            {
                "success": False,
                "message": "Bu servis varsayilan olarak yalnizca yerel istemcilere aciktir.",
            },
            status_code=403,
        )
    return await call_next(request)


def get_reader() -> easyocr.Reader:
    global reader
    if reader is None:
        reader = easyocr.Reader(["en"], gpu=False)
    return reader


def preprocess_versions(image: np.ndarray) -> list[tuple[str, np.ndarray]]:
    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
    scaled = cv2.resize(gray, None, fx=3, fy=3, interpolation=cv2.INTER_CUBIC)
    blurred = cv2.GaussianBlur(scaled, (3, 3), 0)
    _, threshold = cv2.threshold(blurred, 0, 255, cv2.THRESH_BINARY + cv2.THRESH_OTSU)
    adaptive = cv2.adaptiveThreshold(
        blurred,
        255,
        cv2.ADAPTIVE_THRESH_GAUSSIAN_C,
        cv2.THRESH_BINARY,
        31,
        8,
    )

    return [
        ("original", image),
        ("gray", gray),
        ("scaled", scaled),
        ("threshold", threshold),
        ("adaptive_threshold", adaptive),
    ]


def read_candidates(image: np.ndarray) -> list[dict[str, Any]]:
    ocr = get_reader()
    candidates: list[dict[str, Any]] = []

    for method, version in preprocess_versions(image):
        results = ocr.readtext(
            version,
            detail=1,
            paragraph=False,
            allowlist=ALLOWLIST,
            decoder="greedy",
        )

        parts: list[tuple[float, str, float]] = []
        for box, text, confidence in results:
            clean = clean_text(text)
            corrected = correct_by_plate_zones(clean)
            if clean:
                candidates.append(
                    {
                        "text": clean,
                        "plate": corrected,
                        "confidence": float(confidence),
                        "method": method,
                        "valid": is_plate(corrected),
                    }
                )

            try:
                x_pos = min(point[0] for point in box)
            except (IndexError, TypeError, ValueError):
                x_pos = 0
            parts.append((x_pos, clean, float(confidence)))

        joined = clean_text("".join(part for _, part, _ in sorted(parts, key=lambda item: item[0])))
        if joined:
            candidates.append(
                {
                    "text": joined,
                    "plate": correct_by_plate_zones(joined),
                    "confidence": max((conf for _, _, conf in parts), default=0.0),
                    "method": f"{method}_joined",
                    "valid": is_plate(correct_by_plate_zones(joined)),
                }
            )

    return candidates


def choose_best(candidates: list[dict[str, Any]]) -> dict[str, Any] | None:
    if not candidates:
        return None

    grouped: dict[str, list[dict[str, Any]]] = defaultdict(list)
    for candidate in candidates:
        plate = clean_text(candidate.get("plate", ""))
        if plate:
            grouped[plate].append(candidate)

    scored = []
    repeats = Counter(candidate["plate"] for candidate in candidates if candidate.get("plate"))
    for plate, items in grouped.items():
        max_conf = max(float(item.get("confidence", 0.0)) for item in items)
        valid = is_plate(plate)
        score = (100 if valid else 0) + repeats[plate] * 8 + max_conf * 10
        scored.append((score, valid, max_conf, plate, items[0]))

    scored.sort(reverse=True, key=lambda item: (item[0], item[1], item[2]))
    _, valid, max_conf, plate, source = scored[0]
    return {
        "success": bool(valid),
        "plate": plate if valid else "",
        "confidence": round(max_conf, 4) if valid else 0,
        "method": "EasyOCR",
        "source_method": source.get("method", "unknown"),
    }


@app.get("/health")
def health() -> dict[str, str | int]:
    return {
        "status": "ok",
        "network_scope": "remote-opt-in" if ALLOW_REMOTE else "local-only",
        "max_upload_bytes": MAX_UPLOAD_BYTES,
        "max_image_pixels": MAX_IMAGE_PIXELS,
    }


@app.post("/read-plate")
async def read_plate(image: Annotated[UploadFile, File()]) -> JSONResponse:
    if not is_supported_content_type(image.content_type):
        return JSONResponse(
            {
                "success": False,
                "plate": "",
                "confidence": 0,
                "message": "Desteklenmeyen dosya turu. JPEG, PNG, WebP veya BMP kullanin.",
                "candidates": [],
            },
            status_code=415,
        )

    try:
        data = await read_limited_upload(image, MAX_UPLOAD_BYTES)
    except UploadTooLargeError:
        return JSONResponse(
            {
                "success": False,
                "plate": "",
                "confidence": 0,
                "message": f"Goruntu {MAX_UPLOAD_BYTES} bayt yukleme sinirini asiyor.",
                "candidates": [],
            },
            status_code=413,
        )

    if not data:
        return JSONResponse(
            {
                "success": False,
                "plate": "",
                "confidence": 0,
                "message": "Bos dosya yuklenemez.",
                "candidates": [],
            },
            status_code=400,
        )

    buffer = np.frombuffer(data, np.uint8)
    frame = cv2.imdecode(buffer, cv2.IMREAD_COLOR)

    if frame is None:
        return JSONResponse(
            {
                "success": False,
                "plate": "",
                "confidence": 0,
                "message": "Goruntu okunamadi.",
                "candidates": [],
            },
            status_code=400,
        )

    height, width = frame.shape[:2]
    if height * width > MAX_IMAGE_PIXELS:
        return JSONResponse(
            {
                "success": False,
                "plate": "",
                "confidence": 0,
                "message": f"Cozunurluk {MAX_IMAGE_PIXELS} piksel sinirini asiyor.",
                "candidates": [],
            },
            status_code=413,
        )

    candidates = read_candidates(frame)
    best = choose_best(candidates)
    compact_candidates = sorted(
        candidates,
        key=lambda item: (item.get("valid", False), item.get("confidence", 0.0)),
        reverse=True,
    )[:12]

    if best and best["success"]:
        return JSONResponse(
            {
                "success": True,
                "plate": best["plate"],
                "confidence": best["confidence"],
                "method": best["method"],
                "source_method": best["source_method"],
                "candidates": compact_candidates,
            }
        )

    return JSONResponse(
        {
            "success": False,
            "plate": "",
            "confidence": 0,
            "method": "EasyOCR",
            "message": "Plaka okunamadi.",
            "candidates": compact_candidates,
        }
    )
