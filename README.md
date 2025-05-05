# Kallehauge CLI Tools

Right off the bat: this is a super selfish project catered towards myself.

My aim is to start creating useful CLI tools to improve different workflows related to my day-to-day work.
The only reason it's written in C#/.NET is because it's a good starting project to get familiar with a new language.

## Commands

### `git cleanup`

Cleans up local Git repository by interactively prompting to delete branches that are not the current branch (e.g. if you're located on `main`, then `main` will never be shown as an option to be deleted).

#### Examples

You can find all of these - and explanations for each option - if you run `kalle git`:

```bash
kalle git cleanup --exclude main develop     # Interactive branch deletion while excluding certain branches from the list
kalle git cleanup -f                         # Force delete all branches except the one you've checked out
kalle git cleanup -f --exclude main develop  # Force delete all branches except the ones in the "exclude" option
kalle git cleanup --dry-run -f               # Show branches that would be deleted
```

### `git init`

Interactively configures useful global Git aliases to streamline common Git operations.

This command checks your global `~/.gitconfig` file and prompts `[Y/n]` (default Yes) before adding or updating the following aliases:

```
[alias]
    co = checkout
    ci = commit
    st = status
    br = branch
    amend = commit --amend
    cob = checkout -B
    nuke = reset --hard
    # The following alias is added dynamically:
    cleanup = !"<path_to_this_executable>" git cleanup
```

The `cleanup` alias points directly to the `kalle git cleanup` command using the specific path where you placed the executable. The `!` prefix tells Git to run the command in the shell.

If an alias already exists with the correct command, it will be skipped automatically. If an alias exists but points to a different command, you will be prompted to update it.

After running this command, you can use the configured aliases directly with the standard `git` command.

#### Examples

```bash
# Run this once to interactively set up the aliases globally
kalle git init

# After configuration, you can use the aliases:
git st           # Equivalent to 'git status'
git co my-branch # Equivalent to 'git checkout my-branch'
git ci -m "Msg"  # Equivalent to 'git commit -m "Msg"'
git cleanup      # Equivalent to running '<path_to_this_executable> git cleanup' (e.g., 'kalle git cleanup')
...
```

## Installation

I'm building the executable locally (`dotnet publish`) but I've [setup a workflow to create executable files](.github/workflows/release.yml) if you want to try it out. Please leave feedback or create a PR to fix the installations instructions if they're not sufficient.

Be warned: I'm currently working on OSX, so I haven't tested the Linux or Windows build.

### OSX

Heads up in advance: I'm not a verified developer (read: I do not have an app store developer key to sign the executable with, or how that would work outside of the app store), so the path to allow the executable file is a bit annoying.

Everything is fully transparent though, so you can verify that the `release.yml` GitHub workflow is responsible for creating the file with the dotnet tool and all the included code is part of this repository, so feel free to give it a look before proceeding!

1. [Go to the latest release](https://github.com/kallehauge/terminal-commands/releases/latest)
1. Download `KallehaugeTerminalCommands-osx-x64`
1. Give the file execution permission
   * _9/10 times, `chmod +x KallehaugeTerminalCommands-osx-x64` is what you need._
1. Run `~/Downloads/KallehaugeTerminalCommands-osx-x64`
1. Depending on your `Settings > Privacy & Security` settings, then you should see a box with the message _"macos cannot verify that this app is free from malware" (or something similar)_
1. Go to `Settings > Privacy & Security`
1. Close to the bottom of the page you should see this option:
   * _This will only show if you've tried to run the executable in step 4_

![OSX Privacy & Security settings](/docs/images/allow-osx.png)

9. Press "Allow Anyway"
1. (Optional) Move the executable to `~/bin/` and name it what you'd like the command to be
   * _E.g. `mv ~/Downloads/KallehaugeTerminalCommands-osx-x64 ~/bin/kalle` means you'd call it like this: `kalle git cleanup`_
1. Verify that `$HOME/bin` is part of your `$PATH`
   * _You'd typically find it in `.zshrc`; if not, add `export PATH="$HOME/bin:$PATH"`_
1. That's it. Have fun!

### Linux

1. [Go to the latest release](https://github.com/kallehauge/terminal-commands/releases/latest)
1. Download `KallehaugeTerminalCommands-linux-x64`
1. Move it to a bin (and rename it): `mv ~/Downloads/KallehaugeTerminalCommands-linux-x64 ~/bin/{name}`
   * _I assume you have previous knowledge about $PATH, but you can run something like `echo $PATH | grep $HOME/bin` to verify if it's included or not._
1. Run the bin, and have fun!

### Windows

I haven't used Windows for anything programming related since I first learned HTML and installed WAMP or XAMPP. So I'm not much help here. [I assume you start by downloading the `.exe` file](https://github.com/kallehauge/terminal-commands/releases/latest) 😄 Basically me many years ago on Windows 👇

![Windows 90s kid](/docs/images/windows-ok.gif)
