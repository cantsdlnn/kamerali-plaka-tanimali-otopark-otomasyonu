import asyncio

import pytest
from core import (
    UploadTooLargeError,
    clean_text,
    correct_by_plate_zones,
    is_loopback_host,
    is_plate,
    is_supported_content_type,
    read_limited_upload,
)


class FakeUpload:
    def __init__(self, data: bytes):
        self.data = data

    async def read(self, size: int = -1) -> bytes:
        return self.data if size < 0 else self.data[:size]


@pytest.mark.parametrize(
    ("raw", "expected"),
    [
        ("34 abc 123", "34ABC123"),
        ("06-xy-2026", "06XY2026"),
        (None, ""),
    ],
)
def test_clean_text(raw, expected):
    assert clean_text(raw) == expected


@pytest.mark.parametrize("plate", ["01A12", "34ABC123", "81ZZ9999"])
def test_accepts_valid_turkish_plates(plate):
    assert is_plate(plate)


@pytest.mark.parametrize("plate", ["00ABC123", "82ABC123", "34ABCD12", "ABC123"])
def test_rejects_invalid_turkish_plates(plate):
    assert not is_plate(plate)


def test_corrects_common_ocr_confusions_by_plate_zone():
    assert correct_by_plate_zones("O6ABCI23") == "06ABC123"


@pytest.mark.parametrize("content_type", ["image/jpeg", "image/png; charset=binary", "IMAGE/WEBP"])
def test_accepts_supported_content_types(content_type):
    assert is_supported_content_type(content_type)


@pytest.mark.parametrize("content_type", [None, "", "text/plain", "application/octet-stream"])
def test_rejects_unsupported_content_types(content_type):
    assert not is_supported_content_type(content_type)


def test_upload_reader_enforces_byte_limit():
    with pytest.raises(UploadTooLargeError):
        asyncio.run(read_limited_upload(FakeUpload(b"123456"), max_bytes=5))


def test_upload_reader_returns_bounded_payload():
    assert asyncio.run(read_limited_upload(FakeUpload(b"12345"), max_bytes=5)) == b"12345"


@pytest.mark.parametrize("host", ["127.0.0.1", "::1", "localhost"])
def test_allows_loopback_hosts(host):
    assert is_loopback_host(host)


@pytest.mark.parametrize("host", [None, "", "192.168.1.10", "example.test"])
def test_rejects_non_loopback_hosts(host):
    assert not is_loopback_host(host)
