#!/usr/bin/env bash

CSPROJ_FILE="JobTitles.csproj"
VERSION=$(grep -oP '(?<=<Version>).*?(?=</Version>)' "$CSPROJ_FILE")

if [[ -z "$VERSION" ]]; then
  echo "Error: Unable to extract version from $CSPROJ_FILE."
  exit 1
fi

if git rev-parse "refs/tags/$VERSION" >/dev/null 2>&1; then
  echo "Error: Git tag '$VERSION' already exists."
  exit 1
fi

if ! git diff --quiet @{u}; then
  echo "Error: There are unpushed commits. Please push them before releasing."
  exit 1
fi

echo "Tagging and pushing version: $VERSION";

git tag -a "$VERSION" -m "Release $VERSION"
git push origin "$VERSION"

echo "Tag '$VERSION' has been created and pushed successfully."
