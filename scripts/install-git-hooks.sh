#!/bin/sh
set -eu

# Run only after reviewing this installer and the hook sources in this checkout.
repo_root=$(git -C "$(dirname "$0")/.." rev-parse --show-toplevel)
common_dir=$(git -C "$repo_root" rev-parse --path-format=absolute --git-common-dir)
hooks_dir="$common_dir/sailing-out-hooks"

mkdir -p "$hooks_dir"
for hook in pre-commit pre-merge-commit pre-push reference-transaction; do
    install -m 755 "$repo_root/.githooks/$hook" "$hooks_dir/$hook"
done

git -C "$repo_root" config --local core.hooksPath "$hooks_dir"
printf 'Installed reviewed Git hooks in %s\n' "$hooks_dir"
