import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import "./index.css";
import Home from "./app/page.tsx";
import { ThemeProvider } from "./components/theme-context.tsx";
import { ConfirmDialogProvider } from "./components/dialog/ConfirmDialogContext.tsx";
import { SuccessOverlayProvider } from "./components/success/index.ts";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <ThemeProvider>
      <ConfirmDialogProvider>
        <SuccessOverlayProvider>
          <Home />
        </SuccessOverlayProvider>
      </ConfirmDialogProvider>
    </ThemeProvider>
  </StrictMode>
);
