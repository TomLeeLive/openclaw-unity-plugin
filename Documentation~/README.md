# OpenClaw Unity Plugin - Documentation

This folder contains comprehensive documentation for the OpenClaw Unity Plugin.

## Document Index

| Document | Description |
|----------|-------------|
| [index.md](index.md) | **Main documentation** - Architecture overview, core components, key patterns, and quick reference |
| [index_KO.md](index_KO.md) | Main documentation (Korean) |
| [DEVELOPMENT.md](DEVELOPMENT.md) | **Development guide** - How to extend tools, add new features, and contribute |
| [DEVELOPMENT_KO.md](DEVELOPMENT_KO.md) | Development guide (Korean) |
| [TESTING.md](TESTING.md) | **Testing guide** - Manual testing procedures for all 55 tools |
| [TESTING_KO.md](TESTING_KO.md) | Testing guide (Korean) |

## File Descriptions

### index.md / index_KO.md
The main entry point for plugin documentation. Contains:
- Plugin overview and key features
- Architecture diagram (Unity Editor ↔ Gateway communication)
- Core component descriptions (EditorBridge, ConnectionManager, Tools, Config, Logger)
- Key implementation patterns (SafeDestroy, tool aliases, reflection)
- Installation summary
- Quick reference tables (endpoints, tool counts)

### DEVELOPMENT.md / DEVELOPMENT_KO.md
Guide for developers who want to extend or contribute to the plugin:
- Project structure and file organization
- How to add new tools to OpenClawTools.cs
- Gateway extension development
- Debugging tips and common issues
- Code style guidelines
- Pull request process

### TESTING.md / TESTING_KO.md
Comprehensive testing procedures:
- Test environment setup
- Tool-by-tool testing commands
- Expected results for each tool
- Editor mode vs Play mode differences
- Troubleshooting test failures

## Related Files (Parent Directory)

| File | Description |
|------|-------------|
| [README.md](../README.md) | Main project README with features, installation, and usage examples |
| [CHANGELOG.md](../CHANGELOG.md) | Version history following Keep a Changelog format |
| [LICENSE](../LICENSE) | MIT License |
| [package.json](../package.json) | Unity package manifest |

## Quick Links

- **GitHub Repository:** https://github.com/TomLeeLive/openclaw-unity-plugin
- **OpenClaw Gateway:** https://github.com/openclaw/openclaw
- **Companion Skill:** https://github.com/TomLeeLive/openclaw-unity-skill

## Version

- **Plugin Version:** 1.3.0
- **Unity Compatibility:** 2021.3+
- **Total Tools:** 50

---

*Last updated: 2026-02-08*
