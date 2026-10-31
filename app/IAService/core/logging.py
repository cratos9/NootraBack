import logging
import logging.config
from logging.handlers import RotatingFileHandler
from pathlib import Path

BASE_DIR = Path(__file__).resolve().parent.parent
LOG_DIR = BASE_DIR / "logs"
LOG_FILE = LOG_DIR / "iaservice.log"

def configure_logging() -> None:
    """Configure application logging for console and file."""

    LOG_DIR.mkdir(parents=True, exist_ok=True)

    logging.config.dictConfig({
        "version": 1,
        "disable_existing_loggers": False,

        "formatters": {
            "standard": {
                "format": ("%(asctime)s | %(levelname)-8s | %(name)s | %(message)s"),
                "datefmt": "%Y-%m-%d %H:%M:%S",
            },
        },

        "handlers": {
            "console": {
                "class": "logging.StreamHandler",
                "level": "DEBUG",
                "formatter": "standard",
                "stream": "ext://sys.stdout",
            },

            "file": {
                "class": "logging.handlers.RotatingFileHandler",
                "level": "INFO",
                "formatter": "standard",
                "filename": str(LOG_FILE),
                "maxBytes": 5 * 1024 * 1024,
                "backupCount": 5,
                "encoding": "utf-8",
            },
        },

        "root": {
            "level": "DEBUG",
            "handlers": ["console", "file"],
        },

        "loggers": {
            "uvicorn.access": {
                "level": "WARNING",
                "propagate": False,
            },
        },
    })


def get_logger(name: str) -> logging.Logger:
    """Return a logger for the requested module."""
    return logging.getLogger(name)
