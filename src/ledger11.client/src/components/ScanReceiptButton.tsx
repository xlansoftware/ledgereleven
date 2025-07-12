// src/components/ScanReceiptButton.tsx
import React, { useState, useRef, ChangeEvent } from "react";
import { Button } from "@/components/ui/button";
import { Camera, Loader2 } from "lucide-react"; // Import Loader2 for loading state
import { Receipt } from "@/lib/types";
import { scanReceipt } from "@/api";
import { scaleImageFile } from "@/lib/imageUtils";

interface ScanReceiptButtonProps {
  onCompletion: (records: Partial<Receipt>) => void; // Callback with the final extracted data
  onError?: (error: Error) => void; // Optional error callback
  buttonText?: string;
  className?: string;
}

// Define target dimensions
const MAX_IMAGE_WIDTH = 480;
const MAX_IMAGE_HEIGHT = 640;

const ScanReceiptButton: React.FC<ScanReceiptButtonProps> = ({
  onCompletion,
  onError,
  buttonText = "Scan Receipt",
  className,
}) => {
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleButtonClick = () => {
    // Trigger the hidden file input only if not already loading
    if (!isLoading) {
      fileInputRef.current?.click();
    }
  };

  const handleFileChange = async (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];

    // Reset file input value immediately so the same file can be selected again if needed
    // Moved this up to ensure it always runs, even if processing fails early
    if (event.target) {
      event.target.value = "";
    }

    if (!file) {
      console.log("No file selected or file selection failed.");
      return; // Exit if no file is selected
    }

    console.log("Image selected:", file.name);
    setIsLoading(true); // Start loading indicator

    try {
      // --- Scale the image before sending ---
      console.log(`Attempting to scale image if larger than ${MAX_IMAGE_WIDTH}x${MAX_IMAGE_HEIGHT}...`);
      const processedFile = await scaleImageFile(file, MAX_IMAGE_WIDTH, MAX_IMAGE_HEIGHT);
      console.log("Sending image to API...");
      const records = await scanReceipt(processedFile); // Call the API function
      console.log("API call successful, received records:", records);
      onCompletion(records); // Call the success callback with results
    } catch (error) {
      console.error("Error scanning receipt:", error);
      // Optionally call an error handler prop
      if (onError) {
        onError(error instanceof Error ? error : new Error(String(error)));
      }
      // Consider showing a user-friendly error message (e.g., using a toast library)
      alert(
        `Error scanning receipt: ${
          error instanceof Error ? error.message : String(error)
        }`
      ); // Simple alert for now
    } finally {
      setIsLoading(false); // Stop loading indicator regardless of success/failure
    }
  };

  // Removed handleScanCompletion as it's no longer needed separately

  return (
    <>
      {/* Hidden file input */}
      <input
        ref={fileInputRef}
        type="file"
        accept="image/*" // Accepts any image type
        capture="environment" // Prioritize back camera on mobile
        onChange={handleFileChange}
        style={{ display: "none" }}
        disabled={isLoading} // Disable input while processing
      />

      {/* Visible Button */}
      <Button
        onClick={handleButtonClick}
        className={className}
        disabled={isLoading} // Disable button while loading
        aria-label="Scan Receipt"
      >
        {isLoading ? (
          <Loader2 className="mr-2 h-4 w-4 animate-spin" /> // Show spinner when loading
        ) : (
          <Camera className="mr-2 h-4 w-4" /> // Show camera icon normally
        )}
        {isLoading ? "Scanning..." : buttonText}{" "}
        {/* Change button text when loading */}
      </Button>
    </>
  );
};

export default ScanReceiptButton;
