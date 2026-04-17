# Security Policy

## Supported versions

Only the current [latest release](https://github.com/itsnateai/CaplockNumlock/releases/latest) receives security fixes. CapsNumTray's self-updater keeps you on the latest automatically — users running older tags should upgrade.

| Version | Supported |
|---------|-----------|
| 2.2.x (latest LTR) | ✅ |
| < 2.2 | ❌ |

## Reporting a vulnerability

**Please do not open public GitHub issues for security reports.**

Preferred channel:
- Open a private report via GitHub Security Advisories: <https://github.com/itsnateai/CaplockNumlock/security/advisories/new>

What to include, if you can:
- A description of the vulnerability and its impact
- Affected version(s)
- Reproduction steps or a proof-of-concept
- Any suggested mitigation

## What to expect

- **Acknowledgement:** within 3 business days.
- **Initial assessment:** within 7 days, covering severity and whether a fix is planned.
- **Fix or mitigation:** timeline depends on severity; typically a patch release within 14 days for high-severity issues.
- **Disclosure:** coordinated with the reporter. The fix will land in a tagged release and be called out in `CHANGELOG.md`. Credit is given unless the reporter prefers anonymity.

## Scope

In scope:
- The CapsNumTray executable and its installers as distributed from the GitHub Releases page.
- The self-update flow (integrity verification, signature handling, download source checks).
- The P/Invoke surface and other Win32 interactions.

Out of scope:
- Vulnerabilities in Windows, .NET, or third-party dependencies — please report those to their respective maintainers.
- Social engineering, physical attacks, or issues requiring attacker code execution already at administrator privilege on the target machine.
