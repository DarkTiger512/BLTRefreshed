import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { I18nProvider, LanguageSelector, normalizeLocale, useI18n } from "./i18n";

function Example() { const { t, number } = useI18n(); return <><LanguageSelector /><output>{t("shortcut.inventory")} · {number(50000)}</output></>; }

describe("Extension localization", () => {
  it("normalizes Twitch locales and regional fallbacks", () => {
    expect(normalizeLocale("pt-PT")).toBe("pt-BR");
    expect(normalizeLocale("zh-TW")).toBe("zh-Hant");
    expect(normalizeLocale("zh-CN")).toBe("zh-Hans");
    expect(normalizeLocale("nl-NL")).toBe("en");
  });

  it("switches language live and remembers the override", () => {
    localStorage.clear();
    render(<I18nProvider><Example /></I18nProvider>);
    fireEvent.change(screen.getByRole("combobox", { name: "Language" }), { target: { value: "de" } });
    expect(screen.getByText(/Inventar/)).toBeInTheDocument();
    expect(document.documentElement.lang).toBe("de");
    expect(localStorage.getItem("blt.extension.locale.v1")).toBe("de");
  });
});
