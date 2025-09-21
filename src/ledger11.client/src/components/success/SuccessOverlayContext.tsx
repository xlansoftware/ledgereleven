import {
  createContext,
  useCallback,
  useContext,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { AnimatePresence } from "framer-motion";
import { DoneComponent } from "./DoneComponent";

type ShowSuccessOptions = {
  duration?: number;
  waitAfter?: number;
  playSound?: boolean;
};

type SuccessOverlayContextType = {
  showSuccess: (options?: ShowSuccessOptions) => Promise<void>;
};

const SuccessOverlayContext = createContext<
  SuccessOverlayContextType | undefined
>(undefined);

export function SuccessOverlayProvider({ children }: { children: ReactNode }) {
  const [visible, setVisible] = useState(false);
  const resolveRef = useRef<(() => void) | null>(null);
  const [options, setOptions] = useState<ShowSuccessOptions>({});

  const showSuccess = useCallback(
    (opts?: ShowSuccessOptions): Promise<void> => {
      return new Promise((resolve) => {
        resolveRef.current = resolve;
        setOptions(opts || {});
        setVisible(true);
      });
    },
    []
  );

  const handleComplete = useCallback(() => {
    setVisible(false);
    setTimeout(() => {
      resolveRef.current?.();
      resolveRef.current = null;
    }, 300);
  }, []);

  return (
    <SuccessOverlayContext.Provider value={{ showSuccess }}>
      {children}
      <AnimatePresence>
        {visible && (
          <DoneComponent
            onComplete={handleComplete}
            duration={options.duration}
            waitAfter={options.waitAfter}
            playSound={options.playSound}
          />
        )}
      </AnimatePresence>
    </SuccessOverlayContext.Provider>
  );
}

// eslint-disable-next-line react-refresh/only-export-components
export function useSuccessOverlay(): SuccessOverlayContextType {
  const context = useContext(SuccessOverlayContext);
  if (!context) {
    throw new Error(
      "useSuccessOverlay must be used within a SuccessOverlayProvider"
    );
  }
  return context;
}
