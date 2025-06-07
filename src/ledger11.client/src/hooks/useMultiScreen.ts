import { useEffect } from "react";

const useMultiScreen = (isMultiScreen: boolean) => {
  useEffect(() => {
    const htmlElement = document.documentElement;

    // Add the class on mount
    if (isMultiScreen) {
      htmlElement.classList.add("multi-screen");
    }

    // Cleanup function to remove the class on unmount
    return () => {
      htmlElement.classList.remove("multi-screen");
    };
  }, [isMultiScreen]);
};

export default useMultiScreen;
