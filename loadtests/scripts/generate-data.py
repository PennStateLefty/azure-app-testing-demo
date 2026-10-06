#!/usr/bin/env python3
"""Generate deterministic JMeter CSV data from LifeCore seed conventions."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "jmeter" / "data"
DATA.mkdir(parents=True, exist_ok=True)

FIRST_CASE = 100001
CASE_COUNT = 2000
FIRST_POLICY = 1000001
POLICY_COUNT = 10000
POLICY_SAMPLE = 2000

LAST_NAMES = [
    "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis",
    "Rodriguez", "Martinez", "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas",
    "Taylor", "Moore", "Jackson", "Martin", "Lee", "Perez", "Thompson", "White",
    "Harris", "Sanchez", "Clark", "Ramirez", "Lewis", "Robinson", "Walker", "Young",
    "Allen", "King", "Wright", "Scott", "Torres", "Nguyen", "Hill", "Flores",
    "Green", "Adams", "Nelson", "Baker", "Hall", "Rivera", "Campbell", "Mitchell",
    "Carter", "Roberts", "Gomez", "Phillips", "Evans", "Turner", "Diaz", "Parker",
    "Cruz", "Edwards", "Collins", "Reyes", "Stewart", "Morris", "Morales", "Murphy",
    "Cook", "Rogers", "Gutierrez", "Ortiz", "Morgan", "Cooper", "Peterson", "Bailey",
    "Reed", "Kelly", "Howard", "Ramos", "Kim", "Cox", "Ward", "Richardson",
    "Watson", "Brooks", "Chavez", "Wood", "James", "Bennett", "Gray", "Mendoza",
    "Ruiz", "Hughes", "Price", "Alvarez", "Castillo", "Sanders", "Patel", "Myers",
    "Long", "Ross", "Foster", "Jimenez", "Powell", "Jenkins", "Perry", "Russell",
]


def write_lines(path: Path, header: str, rows: list[str]) -> None:
    path.write_text(header + "\n" + "\n".join(rows) + "\n", encoding="utf-8")


def main() -> None:
    cases = [f"UW{FIRST_CASE + i:06d}" for i in range(CASE_COUNT)]
    # Spread 2,000 rows across the 10,000-policy range using a stable stride.
    policies = [f"LC{FIRST_POLICY + ((i * 5) % POLICY_COUNT):07d}" for i in range(POLICY_SAMPLE)]
    write_lines(DATA / "cases.csv", "caseNumber", cases)
    write_lines(DATA / "policies.csv", "policyNumber", policies)
    write_lines(DATA / "owner-lastnames.csv", "lastName", LAST_NAMES)
    print(f"Wrote {len(cases)} cases, {len(policies)} policies, {len(LAST_NAMES)} last names to {DATA}")


if __name__ == "__main__":
    main()
