import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import Home from './app/page.tsx'
import { ThemeProvider } from './components/theme-context.tsx'
import { ConfirmDialogProvider } from './components/dialog/ConfirmDialogContext.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ThemeProvider>
      <ConfirmDialogProvider>
        <Home />
      </ConfirmDialogProvider>
    </ThemeProvider>
  </StrictMode>,
)
