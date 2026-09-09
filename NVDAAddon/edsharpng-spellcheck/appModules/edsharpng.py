# NVDA appModule for EdSharpNG
# - Adds native NVDA spelling error reporting for the EdSharpNG editor control.
# - Uses Windows Spell Checking API (MsSpellCheckingFacility) and injects `invalid-spelling`
#   format changes into TextInfo so NVDA can announce errors in the user's language.

from __future__ import annotations

from collections import OrderedDict
import locale
from typing import List, Optional, Tuple

import appModuleHandler
import config
import controlTypes
from logHandler import log
import textInfos

try:
	import languageHandler
except Exception:
	languageHandler = None


try:
	from ctypes import POINTER, c_byte, c_ulong, c_wchar_p

	from comtypes import COMError, GUID, HRESULT, IUnknown, COMMETHOD
	import comtypes.client

	_COMTYPES_OK = True
except Exception:  # NVDA always has comtypes, but keep the module safe.
	_COMTYPES_OK = False


if _COMTYPES_OK:

	class ISpellingError(IUnknown):
		_iid_ = GUID("{B7C82D61-FBE8-4B47-9B27-6C0D2E0DE0A3}")
		_methods_ = [
			COMMETHOD([], HRESULT, "get_StartIndex", (["out"], POINTER(c_ulong), "val")),
			COMMETHOD([], HRESULT, "get_Length", (["out"], POINTER(c_ulong), "val")),
			COMMETHOD([], HRESULT, "get_CorrectiveAction", (["out"], POINTER(c_ulong), "val")),
			COMMETHOD([], HRESULT, "get_Replacement", (["out"], POINTER(c_wchar_p), "val")),
		]


	class IEnumSpellingError(IUnknown):
		_iid_ = GUID("{803E3BD4-2828-4410-8290-418D1D73C762}")
		_methods_ = [
			COMMETHOD(
				[],
				HRESULT,
				"Next",
				(["out"], POINTER(POINTER(ISpellingError)), "val"),
			),
		]


	class ISpellChecker(IUnknown):
		_iid_ = GUID("{B6FD0B71-E2BC-4653-8D05-F197E412770B}")
		_methods_ = [
			COMMETHOD([], HRESULT, "get_LanguageTag", (["out"], POINTER(c_wchar_p), "val")),
			COMMETHOD(
				[],
				HRESULT,
				"Check",
				(["in"], c_wchar_p, "text"),
				(["out"], POINTER(POINTER(IEnumSpellingError)), "val"),
			),
			COMMETHOD([], HRESULT, "Suggest", (["in"], c_wchar_p, "word"), (["out"], POINTER(c_ulong), "val")),
			COMMETHOD([], HRESULT, "Add", (["in"], c_wchar_p, "word")),
			COMMETHOD([], HRESULT, "Ignore", (["in"], c_wchar_p, "word")),
			COMMETHOD([], HRESULT, "AutoCorrect", (["in"], c_wchar_p, "from"), (["in"], c_wchar_p, "to")),
			COMMETHOD([], HRESULT, "GetOptionValue", (["in"], c_wchar_p, "option_id"), (["out"], POINTER(c_byte), "val")),
			COMMETHOD([], HRESULT, "get_OptionIds", (["out"], POINTER(c_ulong), "val")),
			COMMETHOD([], HRESULT, "get_Id", (["out"], POINTER(c_wchar_p), "val")),
			COMMETHOD([], HRESULT, "get_LocalizedName", (["out"], POINTER(c_wchar_p), "val")),
			COMMETHOD([], HRESULT, "add_SpellCheckerChanged", (["in"], POINTER(c_ulong), "handler"), (["out"], POINTER(c_ulong), "event_cookie")),
			COMMETHOD([], HRESULT, "remove_SpellCheckerChanged", (["in"], c_ulong, "event_cookie")),
			COMMETHOD([], HRESULT, "GetOptionDescription", (["in"], c_wchar_p, "optionId"), (["out"], POINTER(c_ulong), "val")),
			COMMETHOD(
				[],
				HRESULT,
				"ComprehensiveCheck",
				(["in"], c_wchar_p, "text"),
				(["out"], POINTER(POINTER(IEnumSpellingError)), "val"),
			),
		]


	class ISpellCheckerFactory(IUnknown):
		_iid_ = GUID("{8E018A9D-2415-4677-BF08-794EA61F94BB}")
		_methods_ = [
			COMMETHOD([], HRESULT, "get_SupportedLanguages", (["out"], POINTER(c_ulong), "val")),
			COMMETHOD([], HRESULT, "IsSupported", (["in"], c_wchar_p, "languageTag"), (["out"], POINTER(c_ulong), "val")),
			COMMETHOD(
				[],
				HRESULT,
				"CreateSpellChecker",
				(["in"], c_wchar_p, "languageTag"),
				(["out"], POINTER(POINTER(ISpellChecker)), "val"),
			),
		]


	CLSID_SpellCheckerFactory = GUID("{7AB36653-1796-484B-BDFA-E74F1DB7C1DC}")


def _guessDefaultLanguageTag() -> Optional[str]:
	lang = None
	if languageHandler is not None:
		try:
			lang = languageHandler.getLanguage()
		except Exception:
			lang = None
	if not lang:
		try:
			lang, _enc = locale.getdefaultlocale()  # e.g. "pl_PL"
		except Exception:
			lang = None
	if not lang:
		return None
	return lang.replace("_", "-")


def _normalizeLanguageTag(languageTag: Optional[str]) -> Optional[str]:
	if not languageTag:
		return None
	tag = languageTag.strip()
	if not tag:
		return None
	# NVDA sometimes returns locales like "pl_PL"; Windows spellcheck expects BCP-47 ("pl-PL").
	return tag.replace("_", "-")


def _injectInvalidSpelling(
	items: textInfos.TextInfo.TextWithFieldsT,
	spellingRanges: List[Tuple[int, int]],
) -> textInfos.TextInfo.TextWithFieldsT:
	"""Inject `invalid-spelling` formatChange commands into an existing TextWithFields list.

	spellingRanges: list of (start, length) ranges over the concatenated text content.
	"""
	if not spellingRanges:
		return items

	events: List[Tuple[int, bool]] = []
	for start, length in spellingRanges:
		if length <= 0:
			continue
		end = start + length
		if end <= start:
			continue
		events.append((start, True))
		events.append((end, False))
	events.sort(key=lambda x: (x[0], 0 if x[1] is False else 1))
	if not events:
		return items

	out: textInfos.TextInfo.TextWithFieldsT = []
	eventIndex = 0
	offset = 0
	spelling = False
	currentFormat = textInfos.FormatField()
	hasCurrentFormat = False

	def _makeFormatField() -> textInfos.FormatField:
		ff = textInfos.FormatField()
		if hasCurrentFormat:
			ff.update(currentFormat)
		ff["invalid-spelling"] = spelling
		return ff

	for item in items:
		if isinstance(item, textInfos.FieldCommand) and item.command == "formatChange":
			hasCurrentFormat = True
			currentFormat = item.field or textInfos.FormatField()
			out.append(textInfos.FieldCommand("formatChange", _makeFormatField()))
			continue
		if isinstance(item, str):
			s = item
			while eventIndex < len(events):
				at, value = events[eventIndex]
				rel = at - offset
				if rel < 0:
					eventIndex += 1
					continue
				if rel > len(s):
					break
				if rel > 0:
					out.append(s[:rel])
					s = s[rel:]
					offset += rel
				spelling = value
				out.append(textInfos.FieldCommand("formatChange", _makeFormatField()))
				eventIndex += 1
			if s:
				out.append(s)
				offset += len(s)
		else:
			out.append(item)

	return out


if _COMTYPES_OK:

	class _SpellCheckEngine:
		_MAX_TEXT = 10000
		_CACHE_SIZE = 256

		def __init__(self):
			self._factory: Optional[ISpellCheckerFactory] = None
			self._factoryFailed = False
			self._spellCheckers = {}
			self._cache: "OrderedDict[Tuple[str, str], List[Tuple[int, int]]]" = OrderedDict()

		def _getFactory(self) -> Optional[ISpellCheckerFactory]:
			if self._factoryFailed:
				return None
			if self._factory is not None:
				return self._factory
			try:
				self._factory = comtypes.client.CreateObject(CLSID_SpellCheckerFactory, interface=ISpellCheckerFactory)
				return self._factory
			except Exception:
				self._factoryFailed = True
				log.warning("EdSharpNG spellcheck: SpellCheckerFactory not available", exc_info=True)
				return None

		def _getSpellChecker(self, languageTag: Optional[str]) -> Optional[ISpellChecker]:
			factory = self._getFactory()
			if not factory:
				return None

			lang = _normalizeLanguageTag(languageTag) or (_guessDefaultLanguageTag() or "")
			if not lang:
				return None

			if lang in self._spellCheckers:
				return self._spellCheckers[lang]

			# Some sources provide "pl" rather than "pl-PL". Try both.
			langCandidates = [lang]
			if "-" in lang:
				langCandidates.append(lang.split("-", 1)[0])
			else:
				# Try upgrading "pl" -> "pl-PL" based on current locale.
				defaultLang = _guessDefaultLanguageTag()
				if defaultLang and defaultLang.lower().startswith(lang.lower() + "-"):
					langCandidates.append(defaultLang)

			for candidate in langCandidates:
				try:
					checker = factory.CreateSpellChecker(candidate)
					self._spellCheckers[candidate] = checker
					return checker
				except Exception:
					continue

			return None

		def check(self, text: str, languageTag: Optional[str]) -> List[Tuple[int, int]]:
			if not text:
				return []
			if len(text) > self._MAX_TEXT:
				return []
			if not any(ch.isalpha() for ch in text):
				return []

			langKey = _normalizeLanguageTag(languageTag) or (_guessDefaultLanguageTag() or "")
			cacheKey = (langKey, text)
			if cacheKey in self._cache:
				self._cache.move_to_end(cacheKey)
				return self._cache[cacheKey]

			checker = self._getSpellChecker(langKey)
			if not checker:
				return []

			try:
				enumErrors = checker.ComprehensiveCheck(text)
			except Exception:
				try:
					enumErrors = checker.Check(text)
				except Exception:
					return []

			ranges: List[Tuple[int, int]] = []
			try:
				while True:
					try:
						err = enumErrors.Next()
					except COMError:
						break
					if not err:
						break
					start = int(err.get_StartIndex())
					length = int(err.get_Length())
					if start >= 0 and length > 0:
						ranges.append((start, length))
			except Exception:
				log.warning("EdSharpNG spellcheck: enumeration failed", exc_info=True)
				ranges = []

			ranges.sort(key=lambda x: x[0])
			merged: List[Tuple[int, int]] = []
			for start, length in ranges:
				end = start + length
				if not merged:
					merged.append((start, end))
					continue
				prevStart, prevEnd = merged[-1]
				if start <= prevEnd:
					merged[-1] = (prevStart, max(prevEnd, end))
				else:
					merged.append((start, end))

			result: List[Tuple[int, int]] = [(s, e - s) for s, e in merged if e > s]

			self._cache[cacheKey] = result
			self._cache.move_to_end(cacheKey)
			while len(self._cache) > self._CACHE_SIZE:
				self._cache.popitem(last=False)
			return result


	_spellEngine = _SpellCheckEngine()


class _EdSharpTextInfoMixin:
	def getTextWithFields(self, formatConfig=None):
		items = super().getTextWithFields(formatConfig=formatConfig)
		if items is None:
			return []
		if not items:
			return items
		if not formatConfig:
			formatConfig = config.conf["documentFormatting"]
		if not (formatConfig.get("reportSpellingErrors2") or formatConfig.get("reportSpellingErrors")):
			return items
		if not _COMTYPES_OK:
			return items

		# Infer language from the first format change, if present.
		lang = None
		for item in items:
			if isinstance(item, textInfos.FieldCommand) and item.command == "formatChange":
				lang = item.field.get("language")
				if lang:
					break

		try:
			text = "".join(t for t in items if isinstance(t, str))
			ranges = _spellEngine.check(text, lang)
			if not ranges:
				return items
			global _injectionLogged
			if not _injectionLogged:
				_injectionLogged = True
				log.info("EdSharpNG spellcheck: spelling errors injected")
			return _injectInvalidSpelling(items, ranges)
		except Exception:
			log.warning("EdSharpNG spellcheck: failed to inject spelling errors", exc_info=True)
			return items


_overlayLogged = False
_injectionLogged = False
_fallbackLogged = 0


class AppModule(appModuleHandler.AppModule):
	def chooseNVDAObjectOverlayClasses(self, obj, clsList):
		try:
			# EdSharpNG uses WinForms RichTextBox (RichEdit/MSFTEDIT). Depending on the NVDA
			# backend/overlay, the reported class name can vary (or be generic).
			windowClassName = getattr(obj, "windowClassName", None) or ""
			windowClass = windowClassName.upper()
			isEditorByClass = any(token in windowClass for token in ("RICHEDIT", "RICHTEXT", "MSFTEDIT"))
			if not isEditorByClass:
				role = getattr(obj, "role", None)
				states = getattr(obj, "states", ()) or ()
				# Fallback: treat any multi-line editable text/document as the editor.
				if role not in (controlTypes.Role.EDITABLETEXT, controlTypes.Role.DOCUMENT):
					return
				if controlTypes.State.MULTILINE not in states:
					return
				global _fallbackLogged
				if _fallbackLogged < 5:
					_fallbackLogged += 1
					log.debug(
						"EdSharpNG spellcheck: overlay fallback candidate (role=%s, class=%s)",
						getattr(role, "name", str(role)),
						windowClassName,
					)
			global _overlayLogged
			if not _overlayLogged:
				_overlayLogged = True
				log.info(
					"EdSharpNG spellcheck: overlay active (class=%s, role=%s)",
					windowClassName,
					getattr(getattr(obj, "role", None), "name", str(getattr(obj, "role", ""))),
				)
			clsList.insert(0, _EdSharpRichEditOverlay)
		except Exception:
			return


class _EdSharpRichEditOverlay:
	@property
	def TextInfo(self):
		baseTextInfo = super().TextInfo
		if not _COMTYPES_OK:
			return baseTextInfo

		# Wrap any TextInfo that implements getTextWithFields (UIA or ITextDocument).
		if getattr(self, "_edsharpMixedBaseTextInfo", None) is not baseTextInfo:
			self._edsharpMixedBaseTextInfo = baseTextInfo
			self._edsharpMixedTextInfo = type(
				"EdSharpTextInfo",
				(_EdSharpTextInfoMixin, baseTextInfo),
				{},
			)
			log.info(
				"EdSharpNG spellcheck: TextInfo wrapped (%s)",
				getattr(baseTextInfo, "__name__", str(baseTextInfo)),
			)
		return self._edsharpMixedTextInfo


log.info("EdSharpNG spellcheck: appModule loaded")
