#!/usr/bin/env python3

import json
import os
import tempfile

# Returns False if the file was changed, True if it didn't need changing,
# and None if the file was inaccessible for some reason.
def format_json_file(filepath, check_only=False):
    try:
        with open(filepath, 'r') as f:
            original_data = f.read()
            parsed_data = json.loads(original_data)
    except FileNotFoundError:
        print(f"File {filepath} not found.")
        return None
    except json.JSONDecodeError:
        print(f"File {filepath} contains invalid JSON.")
        return None

    formatted_data = json.dumps(parsed_data, indent=2)

    if check_only:
        return original_data == formatted_data

    if original_data != formatted_data:
        with tempfile.NamedTemporaryFile('w', delete=False) as temp:
            temp.write(formatted_data)
            temp_filename = temp.name

        os.rename(temp_filename, filepath)

    return False


if __name__ == '__main__':
    import sys
    import argparse

    parser = argparse.ArgumentParser(description="Format JSON files.")
    parser.add_argument("filepaths", nargs='+', help="Path to JSON files to format")
    parser.add_argument("--check", action="store_true", help="Only check if files are correctly formatted")

    args = parser.parse_args()

    all_files_ok = True

    for filepath in args.filepaths:
        success = format_json_file(filepath, args.check)
        if not success:
            all_files_ok = False

    sys.exit(0 if all_files_ok else 1)
