import { Moon, Sun } from "lucide-react";
import { Button } from "./ui/button";
import { useTheme } from "./theme-context";

export function ThemeToggle() {
  const { theme, resolvedTheme, setTheme } = useTheme();

  const toggleTheme = () => {
    setTheme(
      (theme === "default" ? resolvedTheme : theme) === "dark"
        ? "light"
        : "dark"
    );
  };

  return (
    <Button variant="outline" size="icon" onClick={toggleTheme} aria-label="Toggle theme">
      {(theme === "default" ? resolvedTheme : theme) === "dark" ? (
        <Sun />
      ) : (
        <Moon />
      )}
      <span className="sr-only">Toggle theme</span>
    </Button>
  );
}
