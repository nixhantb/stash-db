# Stash

A small in-memory key-value store 

## Features

* Put key-value pairs
* Get values by key
* Delete keys
* Scan keys within a range
* Byte-array keys and values

## Structure

* `Stash.Core` - store implementation
* `Stash.Cli` - command-line interface
* `Stash.Tests` - unit tests

## Run

```bash
dotnet run --project src/Stash.Cli
```

## Test

```bash
dotnet test
```
