"""Dependency-free validation helpers for the local OCR service."""

from __future__ import annotations

import ipaddress
import os
import re
from typing import Protocol

ALLOWLIST = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
PLATE_RE = re.compile(r"^(0[1-9]|[1-7][0-9]|8[01])[A-Z]{1,3}[0-9]{2,4}$")
SUPPORTED_IMAGE_TYPES = frozenset(
    {
        "image/bmp",
        "image/jpeg",
        "image/png",
        "image/webp",
    }
)


class AsyncReadable(Protocol):
    async def read(self, size: int = -1) -> bytes: ...


class UploadTooLargeError(ValueError):
    """Raised when an upload is larger than the configured byte limit."""


def clean_text(text: str | None) -> str:
    if not text:
        return ""
    return re.sub(r"[^0-9A-Z]", "", text.upper())


def is_plate(text: str) -> bool:
    return bool(PLATE_RE.match(clean_text(text)))


def correct_by_plate_zones(text: str) -> str:
    value = clean_text(text)
    if len(value) < 5 or is_plate(value):
        return value

    def to_digit(char: str) -> str:
        return {
            "O": "0",
            "Q": "0",
            "D": "0",
            "I": "1",
            "L": "1",
            "S": "5",
            "B": "8",
        }.get(char, char)

    def to_letter(char: str) -> str:
        return {
            "0": "O",
            "1": "I",
            "5": "S",
            "8": "B",
        }.get(char, char)

    best = value
    best_changes = 999
    for letter_count in range(1, 4):
        digit_count = len(value) - 2 - letter_count
        if digit_count < 2 or digit_count > 4:
            continue

        chars = list(value)
        changes = 0
        for index, char in enumerate(chars):
            old = char
            if index < 2 or index >= 2 + letter_count:
                chars[index] = to_digit(char)
            else:
                chars[index] = to_letter(char)
            if chars[index] != old:
                changes += 1

        candidate = "".join(chars)
        if is_plate(candidate) and changes < best_changes:
            best = candidate
            best_changes = changes

    return best


def is_supported_content_type(content_type: str | None) -> bool:
    if not content_type:
        return False
    normalized = content_type.split(";", maxsplit=1)[0].strip().lower()
    return normalized in SUPPORTED_IMAGE_TYPES


async def read_limited_upload(upload: AsyncReadable, max_bytes: int) -> bytes:
    if max_bytes <= 0:
        raise ValueError("max_bytes must be positive")

    data = await upload.read(max_bytes + 1)
    if len(data) > max_bytes:
        raise UploadTooLargeError(f"upload exceeds {max_bytes} bytes")
    return data


def is_loopback_host(host: str | None) -> bool:
    if not host:
        return False
    normalized = host.strip().split("%", maxsplit=1)[0]
    if normalized.lower() == "localhost":
        return True
    try:
        return ipaddress.ip_address(normalized).is_loopback
    except ValueError:
        return False


def bounded_int_from_env(name: str, default: int, minimum: int, maximum: int) -> int:
    raw_value = os.getenv(name)
    if raw_value is None:
        return default
    try:
        value = int(raw_value)
    except ValueError:
        return default
    return value if minimum <= value <= maximum else default


def truthy_env(name: str) -> bool:
    return os.getenv(name, "").strip().lower() in {"1", "true", "yes"}
