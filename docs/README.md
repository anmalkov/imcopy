# imcopy docs

## How to use

There are two ways how to use imcopy.

### Simple option

This option allows you to copy all of the files from a source folder to a destination folder. You can also control parallelism.

Parameters:

- `--source` or `-s`: Source directory path
- `--destination` or `-d`: Destination directory path
- `--parallel` or `-p`: Degree of parallelism. If option is not specified or left empty, the default value (8) will be used. Specify an integer value for custom parallelism. If you do not want to use parallelism, specify 0 or 1 as a value.

#### Examples

1. Simple Linux command  
  This command will copy all files from `/home/user/data` to `/home/user/archive/data`. The copy process will use default parallelism (8).  
  ```bash
  imcopy --source /home/user/data --destination /home/user/archive/data
  ```

2. Simple Windows command  
  This command will copy all files from `C:\Users\user\data` to `C:\Users\user\archive\data`. The copy process will use custom parallelism (16).  
  ```bash
  .\imcopy.exe --source C:\Users\user\data --destination C:\Users\user\archive\data --parallel 16
  ```

### Advance option

This option allows you to 



## Ignore patterns

### Patterns

The following patterns are supported ([from wikipedia](https://en.wikipedia.org/wiki/Glob_(programming))):
> 
| Wildcard  | Description | Example | Matches | Does not match |
| --------  | ----------- | ------- | ------- | -------------- |
| \* |  matches any number of any characters including none	| Law\*| Law, Laws, or Lawyer	|
| ?	| matches any single character	| ?at	| Cat, cat, Bat or bat	| at |
| [abc] |	matches one character given in the bracket |	[CB]at |	Cat or Bat	| cat or bat |
| [a-z] |	matches one character from the range given in the bracket	| Letter[0-9]	| Letter0, Letter1, Letter2 up to Letter9	| Letters, Letter or Letter10 |
| [!abc] | matches one character that is not given in the bracket | [!C]at | Bat, bat, or cat | Cat |
| [!a-z] | matches one character that is not from the range given in the bracket | Letter[!3-5] | Letter1, Letter2, Letter6 up to Letter9 and Letterx etc. | Letter3, Letter4, Letter5 or Letterxx |

In addition, DotNet Glob also supports:

| Wildcard  | Description | Example | Matches | Does not match |
| --------  | ----------- | ------- | ------- | -------------- |
| `**` |  matches any number of path / directory segments. When used must be the only contents of a segment. | /\*\*/some.\* | /foo/bar/bah/some.txt, /some.txt, or /foo/some.txt	|


### Escaping special characters

Wrap special characters `?, *, [` in square brackets in order to escape them.
You can also use negation when doing this.

Here are some examples:

| Pattern  | Description | Matches |  
| --------  | ----------- | ------- | 
|`/foo/bar[[].baz` | match a `[` after bar | `/foo/bar[.baz` |
|`/foo/bar[!!].baz` | match any character except `!` after bar | `/foo/bar7.baz` |
|`/foo/bar[!]].baz` | match any character except an ] after bar | `/foo/bar7.baz` |
|`/foo/bar[?].baz` | match an `?` after bar | `/foo/bar?.baz` |
|`/foo/bar[*]].baz` | match either a `*` or a `]` after bar | `/foo/bar*.baz`,`/foo/bar].baz` |
|`/foo/bar[*][]].baz` | match `*]` after bar | `/foo/bar*].baz` |

- Wildcard `*`: Matches any sequence of characters within the same directory level. For example, `*.jpg` matches all files with a `.jpg` extension in the same directory level.
- Single-character wildcard `?`: Matches a single character in the specified position. For example, `file?.txt` matches `file1.txt`, `file2.txt`, and so on.
- Character group `[abc]`: Matches any single character in the specified group. For example, `file[123].txt` matches `file1.txt`, `file2.txt`, and `file3.txt`.
- Negated character group `[^abc]`: Matches any single character not in the specified group. For example, `file[^123].txt` matches any file with a name like `fileX.txt` where `X` is not `1`, `2`, or `3`.
- Character range `[a-z]`: Matches any single character within the specified range. For example, `file[a-c].txt` matches `filea.txt`, `fileb.txt`, and `filec.txt`.
- Recursive wildcard `**`: Matches any sequence of directories. For example, `**/*.jpg` matches all `.jpg` files in any directory level.

### Examples

Here are some examples of patterns you can use in the imcopy comfiguration file (imcopy.yaml):

```yaml
ignorePatterns:
  - "*.jpg"         # Ignores all .jpg files in the same directory level
  - "temp"          # Ignores a folder named "temp" and its contents
  - "file?.txt"     # Ignores files like file1.txt, file2.txt, etc.
  - "**/*.bak"      # Ignores all .bak files in any directory level
  - "data/old_*"    # Ignores files starting with "old_" inside the "data" folder
```
