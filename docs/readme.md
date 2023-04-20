## Ignore patterns

- Wildcard *: Matches any sequence of characters within the same directory level. For example, `*.jpg` matches all files with a `.jpg` extension in the same directory level.
- Single-character wildcard ?: Matches a single character in the specified position. For example, `file?.txt` matches `file1.txt`, `file2.txt`, and so on.
- Character group [abc]: Matches any single character in the specified group. For example, file[123].txt matches file1.txt, file2.txt, and file3.txt.
- Negated character group [^abc]: Matches any single character not in the specified group. For example, file[^123].txt matches any file with a name like fileX.txt where X is not 1, 2, or 3.
- Character range [a-z]: Matches any single character within the specified range. For example, file[a-c].txt matches filea.txt, fileb.txt, and filec.txt.
- Recursive wildcard **: Matches any sequence of directories. For example, **/*.jpg matches all .jpg files in any directory level.

Here are some examples of patterns you can use in the appsettings.yaml file:

```yaml
ignorePatterns:
  - "*.jpg"         # Ignores all .jpg files in the same directory level
  - "temp"          # Ignores a folder named "temp" and its contents
  - "file?.txt"     # Ignores files like file1.txt, file2.txt, etc.
  - "**/*.bak"      # Ignores all .bak files in any directory level
  - "data/old_*"    # Ignores files starting with "old_" inside the "data" folder
```
