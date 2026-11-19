# Offline tutorial — EdSharp DRAFT PL/EN

This overlay contains paired Polish and English Markdown for the **unpublished target** PL/EN menu model. It does not alter the source tutorial, installed EdSharp, or user exercises. Each exercise is a separate copy-oriented document; generated HTML is static, offline, and has no external assets.

## Generate and check

From this thin overlay root run:

```text
python3 -m unittest -v tests/test_pretranslation_tutorial.py
python3 tools/build_pretranslation_tutorial.py --source docs/tutorial --output docs/tutorial/html
python3 tools/build_pretranslation_tutorial.py --source docs/tutorial --output docs/tutorial/html --check
```

The generator verifies PL/EN counterparts (seven lesson IDs and exercises matched by number), local source/output links, `lang`, title, exactly one `h1`, `main`, a `#tresc` skip link, heading order, and no external assets. Static/headless validation is not live NVDA evidence and does not prove the documented EdSharp GUI actions.

The focused content delta documents target `Ctrl+,` **Settings/Opcje konfiguracji** selection of a durable snippet folder; `Alt+V`, `Alt+S`, `Alt+Shift+V`, and `Ctrl+Shift+Space`; explicit snippet-library ZIP import/export; the neutral task-toggle help/palette name; View’s preview/wrap order; and the multi-file Transform Files safety boundary. The legacy full help outside this tutorial remains backlog for its owner.
