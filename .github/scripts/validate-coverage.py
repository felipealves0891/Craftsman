#!/usr/bin/env python3

import argparse
import math
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


def read_coverage(path: Path) -> tuple[int, int, int, int]:
    try:
        root = ET.parse(path).getroot()

        if root.tag != "coverage":
            raise ValueError("Formato Cobertura inválido")

        fields = (
            "lines-covered",
            "lines-valid",
            "branches-covered",
            "branches-valid",
        )

        values = [int(root.attrib[field]) for field in fields]
        lc, lv, bc, bv = values

        if lv <= 0 or not (0 <= lc <= lv):
            raise ValueError("Contadores de linhas inválidos")

        if bv < 0 or not (0 <= bc <= bv):
            raise ValueError("Contadores de branches inválidos")

        return lc, lv, bc, bv

    except (ET.ParseError, KeyError, ValueError, OSError) as exc:
        raise ValueError(f"{path}: {exc}") from exc


def percentage(covered: int, valid: int) -> float:
    if valid == 0:
        return 100.0
    return covered * 100.0 / valid


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Validação de cobertura .NET"
    )

    parser.add_argument(
        "--directory",
        default="TestResults"
    )

    parser.add_argument(
        "--min-lines",
        type=float,
        default=80.0
    )

    parser.add_argument(
        "--min-branches",
        type=float,
        default=70.0
    )

    args = parser.parse_args()

    for name, value in (
        ("min-lines", args.min_lines),
        ("min-branches", args.min_branches),
    ):
        if not math.isfinite(value) or not 0 <= value <= 100:
            print(f"::error::{name} deve estar entre 0 e 100")
            return 1

    directory = Path(args.directory)

    if not directory.is_dir():
        print(f"::error::Diretório não encontrado: {directory}")
        return 1

    reports = sorted(
        directory.rglob("coverage.cobertura.xml")
    )

    if not reports:
        print("::error::Nenhum relatório encontrado")
        return 1

    total_lc = 0
    total_lv = 0
    total_bc = 0
    total_bv = 0

    try:
        for report in reports:
            lc, lv, bc, bv = read_coverage(report)

            total_lc += lc
            total_lv += lv
            total_bc += bc
            total_bv += bv

            print(f"\n{report}")
            print(f"  Lines:    {percentage(lc, lv):.2f}%")
            print(f"  Branches: {percentage(bc, bv):.2f}%")

    except ValueError as exc:
        print(f"::error::{exc}")
        return 1

    if total_bv == 0:
        print(
            "::error::Nenhum branch mensurável encontrado. "
            "Não é possível validar cobertura de branches."
        )
        return 1

    lines = percentage(total_lc, total_lv)
    branches = percentage(total_bc, total_bv)

    print("\n=== Coverage Summary ===")
    print(f"Reports: {len(reports)}")
    print(f"Lines: {lines:.2f}% ({total_lc}/{total_lv})")
    print(f"Branches: {branches:.2f}% ({total_bc}/{total_bv})")

    failed = False

    for name, actual, minimum in (
        ("Lines", lines, args.min_lines),
        ("Branches", branches, args.min_branches),
    ):
        if actual + 1e-9 < minimum:
            print(
                f"::error::{name} coverage {actual:.2f}% "
                f"is below {minimum:.2f}%"
            )
            failed = True
        else:
            print(f"{name}: PASS (minimum {minimum:.2f}%)")

    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
