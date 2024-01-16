# imcopy docs

## How to use

There are two ways how to use imcopy.

### Simple option

This option allows you to copy all of the files from a source folder to a single destination folder. You can also control parallelism and files overwrite and remove behavior.

Parameters:

- `--source` or `-s`: Source directory path
- `--destination` or `-d`: Destination directory path
- `--parallel` or `-p`: Degree of parallelism. If option is not specified or left empty, the default value (`8`) will be used. Specify an integer value for custom parallelism. If you do not want to use parallelism (to copy files sequentially), specify 0 or 1 as a value.
- `--overwrite` or `-o`: Files overwrite behavior. If option is not specified, the default value (`ifNewer`) will be used. Possible values:
  - `always`: Overwrite all the files in the destination directory.
  - `ifNewer`: Overwrite a file in the destination directory only if a file in the source directory is newer.
  - `never`: Do NOT copy a file if it already exists in the destination directory.
- `--remove` or `-r`: Files remove behavior. If option is not specified, the default value (`remove`) will be used. Possible values:
  - `keep`: Keep extra files in the destination directory that do NOT exist in the source directory.
  - `remove`: Remove extra files in the destination directory that do NOT exist in the source directory.
- `--verbose` or `-v`: Show details about the copy process.
- `--dry-run`: Do NOT copy or delete files. Just show details what will be copied and deleted. If option is not specified, the default value (`False`) will be used.

#### Examples

1. Simple Linux command  
  This command will copy all files from `/home/user/data` to `/home/user/archive/data`. The copy process will use default parallelism (`8`), default files overwrite behavior (`ifNewer`), and default files remove behavior (`remove`).
  
    ```bash
    imcopy --source /home/user/data --destination /home/user/archive/data --verbose
    ```

2. Simple Windows command  
  This command will copy all files from `C:\Users\user\data` to `C:\Users\user\archive\data`. The copy process will use custom parallelism (`16`), files overwrite behavior (`always`), and files remove behavior (`keep`).
  
    ```bash
    .\imcopy.exe --source C:\Users\user\data --destination C:\Users\user\archive\data --parallel 16 --overwrite always --remove keep
    ```

3. Simple dry-run commnand
  This command will just print out what files will be copied and deleted if you ecexute `imcopy` command to copy files from `/home/user/data` to `/home/user/archive/data`. Nothing will be copied or deleted.

    ```bash
    imcopy --source /home/user/data --destination /home/user/archive/data --dry-run
    ```

### Advance option

This option is designed for users who need to manage complex file copying tasks, including copying files from multiple source directories to various destination directories. The feature is highly customizable, allowing control over parallelism, file overwrite behavior, removal behavior, and specific ignore patterns. All these settings are managed through a YAML configuration file.

Parameters:

- `--file` or `-f`: Path to the YAML configuration file. When this option is used, all other command-line options are ignored.

#### YAML configuration file

The YAML configuration file offers a structured way to specify detailed copy instructions. It consists of the following sections:

- directories
- ignorePatterns
- global settings

##### Directories section

Define a list of copying tasks. Each task includes a source directory, one or more destination directories, and settings for overwrite and removal behavior, as well as name of ignore patterns.

- `directories`: A list of source and destination directories. Each directory has the following properties:
  - `source`: Source directory path
  - `destinations`: A list of destination directory paths
  - `ignorePattern`: Name of the ignore pattern to use for this directory. If not specified no ignore pattern will be used.
  - `overwriteBehavior`: Files overwrite behavior. If not specified, the default value (`ifNewer`) will be used. Possible values:
    - `always`: Overwrite all the files in the destination directory.
    - `ifNewer`: Overwrite a file in the destination directory only if a file in the source directory is newer.
    - `never`: Do NOT copy a file if it already exists in the destination directory.
  - `removeBehavior`: Files remove behavior. If not specified, the default value (`remove`) will be used. Possible values:
    - `keep`: Keep extra files in the destination directory that do NOT exist in the source directory.
    - `remove`: Remove extra files in the destination directory that do NOT exist in the source directory.

Structure:

```yaml
directories:

  - source: <source-path-1>
    destinations:
      - <destination-path-1a>
      - <destination-path-1b>
    ignorePattern: <pattern-name>
    overwriteBehavior: always | ifNewer | never
    removeBehavior: keep | remove

  - source: <source-path-2>
    destinations:
      - <destination-path-2a>
      - <destination-path-2b>
    ignorePattern: <pattern-name>
    overwriteBehavior: always | ifNewer | never
    removeBehavior: keep | remove

# More settings...
```

##### Ignore patterns section

Define a list of ignore patterns. Each pattern has a name and a list of patterns.

- `ignorePatterns`: A list of ignore patterns. Each pattern has the following properties:
  - `name`: Name of the ignore pattern
  - `patterns`: A list of patterns to ignore files in the source directory
    - `pattern`: A pattern

Structure:

```yaml
# More settings...

ignorePatterns:

  - name: <pattern-name-1>
    patterns:
      - <pattern-1a>
      - <pattern-1b>
      - <pattern-1c>

  - name: <pattern-name-2>
    patterns:
      - <pattern-2a>
      - <pattern-2b>
      - <pattern-2c>

# More settings...
```

###### Ignore patterns

The following patterns are supported ([from wikipedia](https://en.wikipedia.org/wiki/Glob_(programming))):
>
| Wildcard  | Description | Example | Matches | Does not match |
| --------  | ----------- | ------- | ------- | -------------- |
| \* |  matches any number of any characters including none | Law\*| Law, Laws, or Lawyer |
| \*\* |  matches any number of path / directory segments. When used must be the only contents of a segment. | bin/\*\* | /foo/bar/bah/some.txt, /some.txt, or /foo/some.txt |
| ? | matches any single character | ?at | Cat, cat, Bat or bat | at |
| [abc] | matches one character given in the bracket | [CB]at | Cat or Bat | cat or bat |
| [a-z] | matches one character from the range given in the bracket | Letter[0-9] | Letter0, Letter1, Letter2 up to Letter9 | Letters, Letter or Letter10 |
| [!abc] | matches one character that is not given in the bracket | [!C]at | Bat, bat, or cat | Cat |
| [!a-z] | matches one character that is not from the range given in the bracket | Letter[!3-5] | Letter1, Letter2, Letter6 up to Letter9 and Letterx etc. | Letter3, Letter4, Letter5 or Letterxx |

**NOTE**: if pattern is not started with `/` or `**` then it will be automatically modified to `**/<pattern>`.
For example, if you specify `*.cs` pattern it will be automatically modified to `**/*.cs`.

###### Escaping special characters

Wrap special characters `?, *, [` in square brackets in order to escape them. You can also use negation when doing this.

Here are some examples:

| Pattern  | Description | Matches |
| --------  | ----------- | ------- |
|`/foo/bar[[].baz` | match a `[` after bar | `/foo/bar[.baz` |
|`/foo/bar[!!].baz` | match any character except `!` after bar | `/foo/bar7.baz` |
|`/foo/bar[!]].baz` | match any character except an ] after bar | `/foo/bar7.baz` |
|`/foo/bar[?].baz` | match an `?` after bar | `/foo/bar?.baz` |
|`/foo/bar[*]].baz` | match either a `*` or a `]` after bar | `/foo/bar*.baz`,`/foo/bar].baz` |
|`/foo/bar[*][]].baz` | match `*]` after bar | `/foo/bar*].baz` |

###### Ignore pattern examples

| Pattern | Description |
| ------- | ----------- |
| `.git/**` | Ignore all files in the `.git` folder and its subfolders |
| `[Bb]in/**` | Ignore all files in the `bin` and `Bin` folders and their subfolders |
| `node_modules/**` | Ignore all files in the `node_modules` folder and its subfolders |
| `README.md` | Ignore all `README.md` files in any directory |
| `/config/.env` | Ignore single `.env` file in the `config` folder |
| `*.cs` | Ignore all files that have `.cs` extension in any directory |
| `index*.html` | Ignore all files that name starts with `index` and have `.html` extension in any directory |
| `key.*` | Ignore all files that have name `key` and any extension in any directory |

##### Global settings section

Define global settings for all the copying tasks.

- `parallelism`: Degree of parallelism. If option is not specified or left empty, the default value (`8`) will be used. Specify an integer value for custom parallelism. If you do not want to use parallelism (to copy files sequentially), specify 0 or 1 as a value.
- `verbose`: Show details about the copy process. If option is not specified, the default value (`False`) will be used.
- `dryRun`: Do NOT copy or delete files. Just show details what will be copied and deleted. If option is not specified, the default value (`False`) will be used.

Structure:

```yaml
# More settings...

parallelism: <integer-value>
verbose: true | false
dryRun: true | false
```

##### Full YAML configuration file example

For Windows:

```yaml
directories:
- source: D:\Temp\imcopy-demo
  destinations:
  - D:\Temp\imcopy-demo-dest11
  - D:\Temp\imcopy-demo-dest12
  ignorePattern: demo
  overwriteBehavior: ifNewer
- source: D:\Temp\imcopy-demo2
  destinations:
  - D:\Temp\imcopy-demo-dest2
  ignorePattern: demo
  overwriteBehavior: ifNewer

ignorePatterns:
- name: demo
  patterns:
  # ignore all files in .git/ directory and its subdirectories
  - ".git/**"
  # ignore all files in Bin or bin directories and its subdirectories
  - "[Bb]in/**"
  # ignore all demo.txt files in any directory
  - "demo.txt"
  # ignore single demo123.txt file in test/ directory
  - "/test/demo123.txt"  
  
parallelism: 8
```
