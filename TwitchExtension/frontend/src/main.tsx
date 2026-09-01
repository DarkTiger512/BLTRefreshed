import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { App } from "./App";
import { ConfigurationApp } from "./components/ConfigurationView";
import { I18nProvider } from "./i18n";
import "./styles.css";

const page = window.location.pathname.split("/").pop()?.toLowerCase();
const content = page === "config.html" ? <ConfigurationApp mode="full" />
  : page === "live-config.html" ? <ConfigurationApp mode="live" /> : <App />;
createRoot(document.getElementById("root")!).render(<StrictMode><I18nProvider>{content}</I18nProvider></StrictMode>);
