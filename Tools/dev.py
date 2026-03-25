#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import os
import re
import shutil
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[1]
CONFIG_DIR = REPO_ROOT / "Assets" / "_Project" / "Config"
SCHEMA_DIR = REPO_ROOT / "Assets" / "_Project" / "ConfigSchema"
GENERATED_DIR = REPO_ROOT / "Assets" / "_Project" / "Scripts" / "Generated" / "Config"
RESOURCE_CONFIG_DIR = REPO_ROOT / "Assets" / "_Project" / "Resources" / "Config"


@dataclass
class ValidationError:
    config_name: str
    message: str


def eprint(message: str) -> None:
    print(message, file=sys.stderr)


def pascal_case(text: str) -> str:
    parts = re.split(r"[^a-zA-Z0-9]+", text)
    return "".join(part[:1].upper() + part[1:] for part in parts if part)


def load_json(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8"))


def discover_config_pairs() -> list[tuple[str, Path, Path]]:
    pairs: list[tuple[str, Path, Path]] = []
    for schema_path in sorted(SCHEMA_DIR.glob("*.schema.json")):
        config_name = schema_path.name.replace(".schema.json", "")
        config_path = CONFIG_DIR / f"{config_name}.json"
        pairs.append((config_name, config_path, schema_path))
    return pairs


def json_type_matches(expected_type: str, value: Any) -> bool:
    if expected_type == "object":
        return isinstance(value, dict)
    if expected_type == "string":
        return isinstance(value, str)
    if expected_type == "number":
        return isinstance(value, (int, float)) and not isinstance(value, bool)
    if expected_type == "integer":
        return isinstance(value, int) and not isinstance(value, bool)
    if expected_type == "boolean":
        return isinstance(value, bool)
    if expected_type == "array":
        return isinstance(value, list)
    return True


def validate_node(path: str, value: Any, schema: dict[str, Any]) -> str | None:
    expected_type = schema.get("type")
    if expected_type and not json_type_matches(expected_type, value):
        return f"{path}: expected '{expected_type}', got '{type(value).__name__}'"

    enum_values = schema.get("enum")
    if isinstance(enum_values, list) and value not in enum_values:
        return f"{path}: value '{value}' is not in enum {enum_values}"

    if expected_type == "object":
        if not isinstance(value, dict):
            return f"{path}: expected object"

        required = schema.get("required", [])
        for key in required:
            if key not in value:
                return f"{path}.{key}: required property missing"

        properties = schema.get("properties", {})
        additional = schema.get("additionalProperties", True)

        for key, child_value in value.items():
            if key not in properties:
                if additional is False:
                    return f"{path}.{key}: additional property is not allowed"
                continue
            err = validate_node(f"{path}.{key}", child_value, properties[key])
            if err:
                return err

    if expected_type == "array":
        item_schema = schema.get("items")
        if item_schema and isinstance(value, list):
            for idx, item in enumerate(value):
                err = validate_node(f"{path}[{idx}]", item, item_schema)
                if err:
                    return err

    return None


def validate_single_config(config_name: str, config_path: Path, schema_path: Path) -> ValidationError | None:
    if not config_path.exists():
        return ValidationError(config_name, f"Missing config file: {config_path}")
    if not schema_path.exists():
        return ValidationError(config_name, f"Missing schema file: {schema_path}")

    config = load_json(config_path)
    schema = load_json(schema_path)
    error = validate_node("$", config, schema)
    if error:
        return ValidationError(config_name, error)
    return None


def validate_semantic_rules(config_name: str, config: Any) -> list[str]:
    if config_name == "autochess_content":
        return validate_autochess_semantics(config)
    if config_name == "game_settings":
        return validate_game_settings_semantics(config)
    if config_name == "localization_texts":
        return validate_localization_texts_semantics(config)
    return []


def validate_integer_range(
    value: Any,
    path: str,
    errors: list[str],
    minimum: int,
) -> None:
    if not isinstance(value, int) or isinstance(value, bool):
        errors.append(f"{path}: expected integer")
        return

    if value < minimum:
        errors.append(f"{path}: must be >= {minimum}")


def validate_autochess_semantics(config: Any) -> list[str]:
    if not isinstance(config, dict):
        return ["$: expected object"]

    errors: list[str] = []
    metadata = config.get("metadata", {})
    if isinstance(metadata, dict):
        validate_integer_range(metadata.get("startingGold"), "$.metadata.startingGold", errors, 0)
        validate_integer_range(metadata.get("startingHp"), "$.metadata.startingHp", errors, 1)
        validate_integer_range(metadata.get("benchSize"), "$.metadata.benchSize", errors, 1)
        validate_integer_range(metadata.get("boardSize"), "$.metadata.boardSize", errors, 1)
        validate_integer_range(metadata.get("shopSlots"), "$.metadata.shopSlots", errors, 1)
        validate_integer_range(metadata.get("refreshCost"), "$.metadata.refreshCost", errors, 0)
        validate_integer_range(metadata.get("winGold"), "$.metadata.winGold", errors, 0)
        validate_integer_range(metadata.get("lossHpBase"), "$.metadata.lossHpBase", errors, 1)
        validate_integer_range(metadata.get("maxBattleTurns"), "$.metadata.maxBattleTurns", errors, 1)

    skills = config.get("skills", [])
    skill_ids: set[str] = set()
    if isinstance(skills, list):
        for idx, skill in enumerate(skills):
            if not isinstance(skill, dict):
                continue
            skill_id = str(skill.get("id", "")).strip()
            if not skill_id:
                errors.append(f"$.skills[{idx}].id: skill id must not be empty")
                continue
            if skill_id in skill_ids:
                errors.append(f"$.skills[{idx}].id: duplicate skill id '{skill_id}'")
                continue
            skill_ids.add(skill_id)

    units = config.get("units", [])
    unit_ids: set[str] = set()
    if isinstance(units, list):
        for idx, unit in enumerate(units):
            if not isinstance(unit, dict):
                continue
            unit_id = str(unit.get("id", "")).strip()
            if not unit_id:
                errors.append(f"$.units[{idx}].id: unit id must not be empty")
                continue
            if unit_id in unit_ids:
                errors.append(f"$.units[{idx}].id: duplicate unit id '{unit_id}'")
                continue
            unit_ids.add(unit_id)

        for idx, unit in enumerate(units):
            if not isinstance(unit, dict):
                continue
            skill_id = str(unit.get("skillId", "")).strip()
            if skill_id and skill_id not in skill_ids:
                errors.append(
                    f"$.units[{idx}].skillId: references missing skill '{skill_id}'"
                )

    if not unit_ids:
        errors.append("$.units: at least one unit is required")

    shop_rules = config.get("shopRules", {})
    offer_unit_ids = shop_rules.get("offerUnitIds", []) if isinstance(shop_rules, dict) else []
    if isinstance(offer_unit_ids, list):
        if len(offer_unit_ids) == 0:
            errors.append("$.shopRules.offerUnitIds: must include at least one unit id")
        for idx, offer_id in enumerate(offer_unit_ids):
            offer = str(offer_id).strip()
            if offer and offer not in unit_ids:
                errors.append(
                    f"$.shopRules.offerUnitIds[{idx}]: references missing unit '{offer}'"
                )

    waves = config.get("waves", [])
    rounds: set[int] = set()
    if isinstance(waves, list):
        for wave_idx, wave in enumerate(waves):
            if not isinstance(wave, dict):
                continue
            round_value = wave.get("round")
            if isinstance(round_value, bool) or not isinstance(round_value, int):
                errors.append(f"$.waves[{wave_idx}].round: expected integer")
            else:
                if round_value < 1:
                    errors.append(f"$.waves[{wave_idx}].round: must be >= 1")
                if round_value in rounds:
                    errors.append(f"$.waves[{wave_idx}].round: duplicate round '{round_value}'")
                rounds.add(round_value)

            enemy_ids = wave.get("enemyUnitIds", [])
            if isinstance(enemy_ids, list):
                for enemy_idx, enemy_id in enumerate(enemy_ids):
                    enemy = str(enemy_id).strip()
                    if enemy and enemy not in unit_ids:
                        errors.append(
                            f"$.waves[{wave_idx}].enemyUnitIds[{enemy_idx}]: "
                            f"references missing unit '{enemy}'"
                        )

    return errors


def validate_game_settings_semantics(config: Any) -> list[str]:
    if not isinstance(config, dict):
        return ["$: expected object"]

    errors: list[str] = []
    ui = config.get("ui", {})
    if not isinstance(ui, dict):
        errors.append("$.ui: expected object")
        return errors

    language = str(ui.get("language", "")).strip()
    if language not in {"en", "zh-Hans"}:
        errors.append("$.ui.language: must be one of ['en', 'zh-Hans']")
    return errors


def validate_localization_texts_semantics(config: Any) -> list[str]:
    if not isinstance(config, dict):
        return ["$: expected object"]

    errors: list[str] = []

    default_language = str(config.get("defaultLanguage", "")).strip()
    if default_language not in {"en", "zh-Hans"}:
        errors.append("$.defaultLanguage: must be one of ['en', 'zh-Hans']")

    languages = config.get("languages", [])
    language_codes: set[str] = set()
    if isinstance(languages, list):
        for idx, item in enumerate(languages):
            if not isinstance(item, dict):
                errors.append(f"$.languages[{idx}]: expected object")
                continue
            code = str(item.get("code", "")).strip()
            display_name = str(item.get("displayName", "")).strip()
            if code not in {"en", "zh-Hans"}:
                errors.append(f"$.languages[{idx}].code: unsupported language code '{code}'")
                continue
            if code in language_codes:
                errors.append(f"$.languages[{idx}].code: duplicate language code '{code}'")
            language_codes.add(code)
            if not display_name:
                errors.append(f"$.languages[{idx}].displayName: must not be empty")
    else:
        errors.append("$.languages: expected array")

    entries = config.get("entries", [])
    entry_keys: set[str] = set()
    if isinstance(entries, list):
        for idx, item in enumerate(entries):
            if not isinstance(item, dict):
                errors.append(f"$.entries[{idx}]: expected object")
                continue
            key = str(item.get("key", "")).strip()
            en_text = str(item.get("en", "")).strip()
            zh_text = str(item.get("zhHans", "")).strip()
            if not key:
                errors.append(f"$.entries[{idx}].key: must not be empty")
                continue
            if key in entry_keys:
                errors.append(f"$.entries[{idx}].key: duplicate key '{key}'")
                continue
            entry_keys.add(key)

            if not en_text:
                errors.append(f"$.entries[{idx}].en: must not be empty")
            if not zh_text:
                errors.append(f"$.entries[{idx}].zhHans: must not be empty")
    else:
        errors.append("$.entries: expected array")

    if len(entry_keys) == 0:
        errors.append("$.entries: at least one translation entry is required")

    return errors


def validate_all_configs() -> list[ValidationError]:
    errors: list[ValidationError] = []
    for config_name, config_path, schema_path in discover_config_pairs():
        issue = validate_single_config(config_name, config_path, schema_path)
        if issue:
            errors.append(issue)
            continue

        config = load_json(config_path)
        semantic_errors = validate_semantic_rules(config_name, config)
        for message in semantic_errors:
            errors.append(ValidationError(config_name, message))
    return errors


def csharp_type_for_schema(
    parent_class: str,
    field_name: str,
    field_schema: dict[str, Any],
    sample_value: Any,
    generated_classes: list[str],
) -> str:
    field_type = field_schema.get("type")
    if field_type == "string":
        return "string"
    if field_type == "boolean":
        return "bool"
    if field_type == "integer":
        return "int"
    if field_type == "number":
        return "float"
    if field_type == "object":
        child_class = f"{pascal_case(field_name)}Config"
        emit_object_class(child_class, field_schema, sample_value or {}, generated_classes)
        return child_class
    if field_type == "array":
        item_schema = field_schema.get("items", {})
        item_type = csharp_type_for_schema(parent_class, field_name + "Item", item_schema, None, generated_classes)
        return f"{item_type}[]"
    return "string"


def csharp_literal(csharp_type: str, value: Any) -> str:
    if csharp_type == "string":
        safe = str(value if value is not None else "").replace("\\", "\\\\").replace('"', '\\"')
        return f"\"{safe}\""
    if csharp_type == "bool":
        return "true" if bool(value) else "false"
    if csharp_type == "int":
        return str(int(value if value is not None else 0))
    if csharp_type == "float":
        num = float(value if value is not None else 0.0)
        text = f"{num:.6f}".rstrip("0").rstrip(".")
        if "." not in text:
            text += ".0"
        return f"{text}f"
    if csharp_type.endswith("[]"):
        return "System.Array.Empty<" + csharp_type.replace("[]", "") + ">()"
    return f"new {csharp_type}()"


def emit_object_class(
    class_name: str,
    schema: dict[str, Any],
    sample_value: Any,
    generated_classes: list[str],
) -> None:
    if any(block.startswith(f"[Serializable]\npublic sealed class {class_name}\n") for block in generated_classes):
        return

    lines: list[str] = []
    lines.append("[Serializable]")
    lines.append(f"public sealed class {class_name}")
    lines.append("{")

    properties = schema.get("properties", {})
    if not properties:
        lines.append("}")
        generated_classes.append("\n".join(lines))
        return

    sample_object = sample_value if isinstance(sample_value, dict) else {}
    for field_name, field_schema in properties.items():
        field_sample = sample_object.get(field_name)
        csharp_type = csharp_type_for_schema(class_name, field_name, field_schema, field_sample, generated_classes)
        literal = csharp_literal(csharp_type, field_sample)
        lines.append(f"    public {csharp_type} {field_name} = {literal};")

    lines.append("}")
    generated_classes.append("\n".join(lines))


def generate_csharp(config_name: str, schema: dict[str, Any], sample: dict[str, Any]) -> tuple[str, str]:
    class_name = schema.get("title") or f"{pascal_case(config_name)}Config"
    generated_classes: list[str] = []
    emit_object_class(class_name, schema, sample, generated_classes)

    blocks = [
        "using System;",
        "",
        "namespace Tsukuyomi.Generated.Config",
        "{",
    ]
    for idx, class_block in enumerate(generated_classes):
        indented = "\n".join(f"    {line}" if line else "" for line in class_block.splitlines())
        blocks.append(indented)
        if idx != len(generated_classes) - 1:
            blocks.append("")
    blocks.append("}")
    blocks.append("")

    file_name = f"{class_name}.g.cs"
    return file_name, "\n".join(blocks)


def write_or_check(path: Path, content: str, check_only: bool) -> bool:
    existing = path.read_text(encoding="utf-8") if path.exists() else None
    if existing == content:
        return True

    if check_only:
        return False

    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8", newline="\n")
    return True


def sync_resource_copy(config_name: str, config_path: Path, check_only: bool) -> bool:
    target = RESOURCE_CONFIG_DIR / config_path.name
    content = config_path.read_text(encoding="utf-8")
    return write_or_check(target, content, check_only)


def command_validate_config(_: argparse.Namespace) -> int:
    errors = validate_all_configs()
    if errors:
        for issue in errors:
            eprint(f"[validate-config] {issue.config_name}: {issue.message}")
        return 1

    print("[validate-config] OK")
    return 0


def command_generate_config(args: argparse.Namespace) -> int:
    validation_errors = validate_all_configs()
    if validation_errors:
        for issue in validation_errors:
            eprint(f"[generate-config] {issue.config_name}: {issue.message}")
        return 1

    check_only = args.check
    all_ok = True

    for config_name, config_path, schema_path in discover_config_pairs():
        schema = load_json(schema_path)
        config = load_json(config_path)
        file_name, generated = generate_csharp(config_name, schema, config)
        target = GENERATED_DIR / file_name

        if not write_or_check(target, generated, check_only):
            all_ok = False
            eprint(f"[generate-config] Generated file out-of-date: {target}")

        if not sync_resource_copy(config_name, config_path, check_only):
            all_ok = False
            eprint(f"[generate-config] Resource copy out-of-date: {RESOURCE_CONFIG_DIR / config_path.name}")

    if not all_ok:
        return 1

    print("[generate-config] OK")
    return 0


def command_guard_architecture(_: argparse.Namespace) -> int:
    problems: list[str] = []

    for forbidden in (CONFIG_DIR.glob("*.asset"), SCHEMA_DIR.glob("*.asset")):
        for path in forbidden:
            problems.append(f"Business ScriptableObject asset is forbidden: {path}")

    presentation_files = list((REPO_ROOT / "Assets" / "_Project" / "Scripts" / "Presentation").rglob("*.cs"))
    for file_path in presentation_files:
        text = file_path.read_text(encoding="utf-8")
        if "using Tsukuyomi.Infrastructure" in text:
            problems.append(f"Presentation must not reference Infrastructure directly: {file_path}")

    domain_files = list((REPO_ROOT / "Assets" / "_Project" / "Scripts" / "Domain").rglob("*.cs"))
    for file_path in domain_files:
        text = file_path.read_text(encoding="utf-8")
        if "using Tsukuyomi.Application" in text or "using Tsukuyomi.Infrastructure" in text:
            problems.append(f"Domain layer may not depend on Application/Infrastructure: {file_path}")

    application_files = list((REPO_ROOT / "Assets" / "_Project" / "Scripts" / "Application").rglob("*.cs"))
    for file_path in application_files:
        text = file_path.read_text(encoding="utf-8")
        if "using Tsukuyomi.Infrastructure" in text:
            problems.append(f"Application layer may not depend on Infrastructure: {file_path}")
        if "ScriptableObject" in text:
            problems.append(f"Application layer must not contain ScriptableObject usage: {file_path}")

    core_layers = [
        REPO_ROOT / "Assets" / "_Project" / "Scripts" / "Domain",
        REPO_ROOT / "Assets" / "_Project" / "Scripts" / "Application",
        REPO_ROOT / "Assets" / "_Project" / "Scripts" / "Infrastructure",
        REPO_ROOT / "Assets" / "_Project" / "Scripts" / "Presentation",
    ]
    for layer_path in core_layers:
        for file_path in layer_path.rglob("*.cs"):
            text = file_path.read_text(encoding="utf-8")
            if "[SerializeField]" in text:
                problems.append(f"Inspector-driven SerializeField is forbidden in core layers: {file_path}")

    if problems:
        for problem in problems:
            eprint(f"[guard-architecture] {problem}")
        return 1

    print("[guard-architecture] OK")
    return 0


def run_unity_command(args: list[str]) -> int:
    unity_editor = os.getenv("UNITY_EDITOR_PATH")
    if not unity_editor:
        print("[unity] UNITY_EDITOR_PATH is not set, skipping Unity execution.")
        return 0

    process = subprocess.run(
        [unity_editor, *args],
        cwd=str(REPO_ROOT),
        check=False,
    )
    return process.returncode


def command_run_tests(_: argparse.Namespace) -> int:
    preflight = [
        command_validate_config(argparse.Namespace()),
        command_generate_config(argparse.Namespace(check=True)),
        command_guard_architecture(argparse.Namespace()),
    ]
    if any(code != 0 for code in preflight):
        return 1

    test_results = REPO_ROOT / "TestResults"
    test_results.mkdir(exist_ok=True)

    edit_mode_args = [
        "-batchmode",
        "-nographics",
        "-projectPath",
        str(REPO_ROOT),
        "-runTests",
        "-testPlatform",
        "EditMode",
        "-testResults",
        str(test_results / "editmode-results.xml"),
        "-quit",
    ]
    play_mode_args = [
        "-batchmode",
        "-nographics",
        "-projectPath",
        str(REPO_ROOT),
        "-runTests",
        "-testPlatform",
        "PlayMode",
        "-testResults",
        str(test_results / "playmode-results.xml"),
        "-quit",
    ]

    if run_unity_command(edit_mode_args) != 0:
        return 1
    if run_unity_command(play_mode_args) != 0:
        return 1
    return 0


def command_smoke_ui(_: argparse.Namespace) -> int:
    results_dir = REPO_ROOT / "TestResults"
    results_dir.mkdir(exist_ok=True)
    args = [
        "-batchmode",
        "-nographics",
        "-projectPath",
        str(REPO_ROOT),
        "-runTests",
        "-testPlatform",
        "PlayMode",
        "-testFilter",
        "Tsukuyomi.Tests.PlayMode.SmokeUiFlowTests",
        "-testResults",
        str(results_dir / "smoke-ui-results.xml"),
        "-quit",
    ]
    return run_unity_command(args)


def command_sync_resources(_: argparse.Namespace) -> int:
    RESOURCE_CONFIG_DIR.mkdir(parents=True, exist_ok=True)
    for config_file in CONFIG_DIR.glob("*.json"):
        shutil.copy2(config_file, RESOURCE_CONFIG_DIR / config_file.name)
    print("[sync-resources] OK")
    return 0


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Tsukuyomi developer workflows")
    subparsers = parser.add_subparsers(dest="command", required=True)

    parser_validate = subparsers.add_parser("validate-config", help="Validate JSON configs against schema.")
    parser_validate.set_defaults(handler=command_validate_config)

    parser_generate = subparsers.add_parser("generate-config", help="Generate C# DTOs from schemas.")
    parser_generate.add_argument("--check", action="store_true", help="Fail if generated files are out-of-date.")
    parser_generate.set_defaults(handler=command_generate_config)

    parser_guard = subparsers.add_parser("guard-architecture", help="Run architecture policy checks.")
    parser_guard.set_defaults(handler=command_guard_architecture)

    parser_tests = subparsers.add_parser("run-tests", help="Run project test gates.")
    parser_tests.set_defaults(handler=command_run_tests)

    parser_smoke = subparsers.add_parser("smoke-ui", help="Run smoke UI PlayMode tests.")
    parser_smoke.set_defaults(handler=command_smoke_ui)

    parser_sync = subparsers.add_parser("sync-resources", help="Copy config sources into Resources.")
    parser_sync.set_defaults(handler=command_sync_resources)

    return parser


def main() -> int:
    parser = build_parser()
    args = parser.parse_args()
    return args.handler(args)


if __name__ == "__main__":
    raise SystemExit(main())
