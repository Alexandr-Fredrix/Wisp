# Contributing

Issues and pull requests are welcome in Russian or English.

Discuss substantial features first. Use a focused branch and explain behavior changes, validation and limitations. Run:

```sh
python tools/check.py
python -m unittest discover -s tests
```

For game changes, state exact game/API versions, OS, save mode and installed mods. Test new and existing saves. Clearly distinguish static checks from in-game testing.

Never commit game assemblies, personal saves, API tokens or server configuration. Original contributions are accepted under MIT; third-party content needs its original terms and attribution.
