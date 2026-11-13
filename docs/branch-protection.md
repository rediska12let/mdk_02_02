# Настройки защиты main — МДК 02.02 ПР9

Это **регламент настройки**, а не заявление, что правила уже включены.

На GitHub откройте Settings → Rules → Rulesets (или Branches), создайте правило для `main`:
1. Require a pull request before merging.
2. Require at least one approving review.
3. Require status check `build-test` после первого успешного CI запуска.
4. Restrict force pushes and branch deletion.
5. Block direct pushes (в рамках возможностей аккаунта/тарифа).
6. Include administrators, если настройка доступна.

Git LFS для крупных проектных бинарных файлов (не для медицинских данных):
```bash
git lfs install
git lfs track "*.pdf"
git add .gitattributes
git status --short
```
Не используйте LFS для реальных медицинских документов. При утечке секретов одного .gitignore недостаточно: нужна отзыв/ротация секретов и очистка истории.
