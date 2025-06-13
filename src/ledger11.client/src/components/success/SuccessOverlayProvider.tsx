import { useEffect, useState, useCallback } from "react";
import { AnimatePresence } from "framer-motion";
import { DoneComponent } from "./DoneComponent";

let showSuccessInternal: (onComplete?: () => void) => void = () => {};

export function useSuccessOverlayController() {
  const [visible, setVisible] = useState(false);
  const [onComplete, setOnComplete] = useState<() => void>(() => {});

  useEffect(() => {
    showSuccessInternal = (callback?: () => void) => {
      setVisible(true);
      if (callback) setOnComplete(() => callback);
    };
  }, []);

  const handleComplete = useCallback(() => {
    setVisible(false);
    setTimeout(() => {
      onComplete?.();
    }, 300);
  }, [onComplete]);

  return (
    <AnimatePresence>
      {visible && <DoneComponent onComplete={handleComplete} />}
    </AnimatePresence>
  );
}

export function SuccessOverlayProvider() {
  return useSuccessOverlayController();
}

// Export internal function so `showSuccess` can call it
export { showSuccessInternal };
