"""Podsluch MOWY NVDA - wersja 2, przez PUNKT ROZSZERZENIA (nie latanie funkcji).

DLACZEGO WERSJA 2 (pomiar 19.08.2026): wersja 1 podmieniala `speech.speak` i NIE
LAPALA NICZEGO - ani zwyklej mowy, ani wywolania speak_text z mostka. Przyczyna:
w NVDA 2026 wewnetrzne wywolania nie przechodza przez ten symbol (moduly trzymaja
wlasne referencje, a `speech.speak` z pakietu `speech/__init__` to tylko elewacja).
KLASA BLEDU: latanie (monkey-patch) symbolu w pakiecie dziala tylko dla tych, ktorzy
wolaja go PRZEZ TEN pakiet; kto zaimportowal funkcje wczesniej albo siega do
`speech.speech`, ma stara referencje. Wlasciwa droga to oficjalny punkt rozszerzenia
`speech.extensions.speechCanceled` / `pre_speechQueued`, ktory NVDA sam wola.

Zapisuje do %TEMP%\\nvda_mowa.log: znacznik czasu + tekst sekwencji mowy.
"""
import codecs
import os
import time

import globalPluginHandler

SCIEZKA = os.path.join(os.environ.get("TEMP", r"C:\Windows\Temp"), "nvda_mowa.log")


def _zapisz(tekst):
    try:
        with codecs.open(SCIEZKA, "a", encoding="utf-8") as f:
            f.write("%.3f\t%s\n" % (time.time(), tekst))
    except Exception:
        pass


def _opisz(sekwencja):
    czesci = []
    try:
        for el in sekwencja:
            if isinstance(el, str):
                czesci.append(el)
            else:
                czesci.append("<%s>" % el.__class__.__name__)
    except Exception as exc:
        czesci.append("<blad opisu: %s>" % exc)
    return " | ".join(czesci)


class GlobalPlugin(globalPluginHandler.GlobalPlugin):
    def __init__(self):
        super(GlobalPlugin, self).__init__()
        self._podpiete = []
        podpiete_nazwy = []

        # 1) OFICJALNY punkt rozszerzenia: pre_speechQueued (NVDA 2021+)
        try:
            from speech import extensions as se
            if hasattr(se, "pre_speechQueued"):
                se.pre_speechQueued.register(self._na_kolejce)
                self._podpiete.append(("pre_speechQueued", se.pre_speechQueued,
                                       self._na_kolejce))
                podpiete_nazwy.append("pre_speechQueued")
        except Exception as exc:
            _zapisz("<nie udalo sie podpiac pre_speechQueued: %s>" % exc)

        # 2) Zapas: synthDriverHandler.synthSpeak (to widzi KAZDA mowe idaca do
        #    syntezatora, nawet gdy ktos omija warstwe speech)
        try:
            from synthDriverHandler import synthIndexReached  # noqa: F401
            import speech.speech as ss
            self._oryg_speak = ss.speak

            def _speak(sekwencja, *a, **kw):
                _zapisz("SPEAK: " + _opisz(sekwencja))
                return self._oryg_speak(sekwencja, *a, **kw)

            ss.speak = _speak
            self._latka_ss = ss
            podpiete_nazwy.append("speech.speech.speak")
        except Exception as exc:
            _zapisz("<nie udalo sie zalatac speech.speech.speak: %s>" % exc)

        _zapisz("=== PODSLUCH v2 WLACZONY, podpiete: %s ===" % ", ".join(podpiete_nazwy))

    def _na_kolejce(self, speechSequence=None, **kwargs):
        _zapisz("QUEUED: " + _opisz(speechSequence or []))

    def terminate(self):
        try:
            for nazwa, punkt, uchwyt in self._podpiete:
                try:
                    punkt.unregister(uchwyt)
                except Exception:
                    pass
            if hasattr(self, "_latka_ss"):
                self._latka_ss.speak = self._oryg_speak
        finally:
            _zapisz("=== PODSLUCH v2 WYLACZONY ===")
        super(GlobalPlugin, self).terminate()
