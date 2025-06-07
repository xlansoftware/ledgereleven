// src/lib/imageUtils.ts (or similar utility file)

/**
 * Scales an image File down if its dimensions exceed the specified maxWidth or maxHeight,
 * maintaining aspect ratio.
 *
 * @param file The original image File object.
 * @param maxWidth The maximum desired width.
 * @param maxHeight The maximum desired height.
 * @param quality Optional quality setting for JPEG compression (0.0 to 1.0). Defaults to 0.9.
 * @returns A Promise resolving to the scaled File object (or the original if no scaling was needed).
 *          The Promise rejects if there's an error loading or processing the image.
 */
export function scaleImageFile(
  file: File,
  maxWidth: number,
  maxHeight: number,
  quality: number = 0.9
): Promise<File> {
  return new Promise((resolve, reject) => {
    if (!file.type.startsWith("image/")) {
      return reject(new Error("File is not an image."));
    }

    const img = new Image();
    const objectUrl = URL.createObjectURL(file); // Use object URL instead of FileReader

    img.onload = () => {
      const width = img.naturalWidth;
      const height = img.naturalHeight;

      // Revoke object URL after image is loaded
      URL.revokeObjectURL(objectUrl);

      // No scaling needed
      if (width <= maxWidth && height <= maxHeight) {
        return resolve(file);
      }

      const scale = Math.min(maxWidth / width, maxHeight / height);
      const newWidth = Math.round(width * scale);
      const newHeight = Math.round(height * scale);

      const canvas = document.createElement("canvas");
      canvas.width = newWidth;
      canvas.height = newHeight;
      const ctx = canvas.getContext("2d");

      if (!ctx) {
        return reject(new Error("Failed to get 2D context from canvas."));
      }

      ctx.imageSmoothingQuality = "high";
      ctx.drawImage(img, 0, 0, newWidth, newHeight);

      canvas.toBlob(
        (blob) => {
          if (!blob) {
            return reject(new Error("Canvas to Blob conversion failed."));
          }
          const scaledFile = new File([blob], file.name, {
            type: blob.type || file.type,
            lastModified: Date.now(),
          });
          resolve(scaledFile);
        },
        file.type.startsWith("image/jpeg") ? "image/jpeg" : file.type,
        file.type.startsWith("image/jpeg") ? quality : undefined
      );
    };

    img.onerror = (error) => {
      URL.revokeObjectURL(objectUrl); // Cleanup on error too
      reject(new Error(`Image loading error: ${error}`));
    };

    img.src = objectUrl; // Start loading image
  });
}
