# Commit attribution

Before creating each group of feature commits, ask the user to choose the author: Sann Lynn Htun (the user) or phonewinttun-dev. Wait for their choice before committing. This applies to both the backend repository and the nested HsumChaint.Flutter mobile repository.

Use the selected person's verified Git name and email. If phonewinttun-dev's identity is not available from repository history, ask for it. Authentication tokens do not determine commit authorship. Do not change global Git identity settings.

Keep SECRETS.md local and ignored by Git. Never stage, commit, print, or copy its credentials into tracked files or commit messages.

# APK naming

When building the Flutter Android app, name the APK delivered to the user `HsumChaint.apk`. After a successful build, copy the generated APK to `HsumChaint.Flutter/build/app/outputs/flutter-apk/HsumChaint.apk` and share that named file, rather than `app-release.apk` or `app-debug.apk`.

# Commit messages

Use Conventional Commit messages in the form `type(scope): description`, such as `fix(docker): restore local database connectivity` or `docs(build): document APK naming`. Keep each task in a separate commit, grouping only changes needed for that task.
