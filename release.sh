#!/usr/bin/env bash

VERSION="$1"

if [[ -z "$VERSION" ]]; then
  echo "Error: No version provided."
  echo "Usage: $0 <version>"
  exit 1
fi

git tag -a "$VERSION" -m "Release $VERSION"
git push origin "$VERSION"
