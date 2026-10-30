from dataclasses import dataclass
from http import HTTPStatus
from typing import Any


@dataclass(frozen=True)
class ApiError(Exception):
    status_code: int
    code: str
    title: str
    detail: str
    error_type: str
    errors: dict[str, list[str]] | None = None

    def __post_init__(self) -> None:
        Exception.__init__(self, self.detail)


def not_found(resource: str) -> ApiError:
    resource_code = resource.upper().replace(" ", "_")
    return ApiError(
        status_code=HTTPStatus.NOT_FOUND,
        code=f"{resource_code}_NOT_FOUND",
        title=f"{resource} not found",
        detail=f"The requested {resource.lower()} could not be found.",
        error_type=f"https://api.nootra.com/problems/{resource.lower()}-not-found",
    )


def validation_error(errors: dict[str, list[str]]) -> ApiError:
    return ApiError(
        status_code=HTTPStatus.BAD_REQUEST,
        code="VALIDATION_ERROR",
        title="Validation failed",
        detail="One or more fields are invalid.",
        error_type="https://api.nootra.com/problems/validation-error",
        errors=errors,
    )


def problem_payload(error: ApiError, trace_id: str, instance: str) -> dict[str, Any]:
    payload: dict[str, Any] = {
        "type": error.error_type,
        "title": error.title,
        "status": error.status_code,
        "detail": error.detail,
        "instance": instance,
        "code": error.code,
        "traceId": trace_id,
    }
    if error.errors:
        payload["errors"] = error.errors
    return payload
