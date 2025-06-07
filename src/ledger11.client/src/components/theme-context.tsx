import React, { createContext, useContext, useEffect, useState } from 'react'

type Theme = 'light' | 'dark' | 'default'

interface ThemeContextProps {
  theme: Theme
  resolvedTheme: 'light' | 'dark'
  setTheme: (theme: Theme) => void
}

const ThemeContext = createContext<ThemeContextProps | undefined>(undefined)

export const ThemeProvider = ({ children }: { children: React.ReactNode }) => {
  const [theme, setThemeState] = useState<Theme>(() => {
    const stored = localStorage.getItem('theme') as Theme | null
    return stored ?? 'default'
  })

  const [resolvedTheme, setResolvedTheme] = useState<'light' | 'dark'>(() => {
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
  })

  useEffect(() => {
    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)')

    const handleChange = () => {
      const systemTheme = mediaQuery.matches ? 'dark' : 'light'
      setResolvedTheme(systemTheme)

      if (theme === 'default') {
        applyTheme(systemTheme)
      }
    }

    mediaQuery.addEventListener('change', handleChange)
    handleChange()

    return () => mediaQuery.removeEventListener('change', handleChange)
  }, [theme])

  useEffect(() => {
    if (theme === 'default') {
      localStorage.removeItem('theme')
      applyTheme(resolvedTheme)
    } else {
      localStorage.setItem('theme', theme)
      applyTheme(theme)
    }
  }, [theme, resolvedTheme])

  const applyTheme = (t: 'light' | 'dark') => {
    document.documentElement.classList.remove('light', 'dark')
    document.documentElement.classList.add(t)
  }

  const setTheme = (t: Theme) => {
    setThemeState(t)
  }

  return (
    <ThemeContext.Provider value={{ theme, resolvedTheme, setTheme }}>
      {children}
    </ThemeContext.Provider>
  )
}

// eslint-disable-next-line react-refresh/only-export-components
export const useTheme = () => {
  const context = useContext(ThemeContext)
  if (!context) throw new Error('useTheme must be used within ThemeProvider')
  return context
}
