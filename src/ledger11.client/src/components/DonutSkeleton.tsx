// components/DonutSkeleton.tsx
import React from "react";

interface DonutSkeletonProps {
  size?: number; // diameter in pixels
  thickness?: number; // thickness of the donut
}

const DonutSkeleton: React.FC<DonutSkeletonProps> = ({
  size = 100,
  thickness = 10,
}) => {
  const borderWidth = `${thickness}px`;

  return (
    <div
      className="animate-spin rounded-full border-t-muted border-l-transparent border-r-transparent border-b-transparent"
      style={{
        width: `${size}px`,
        height: `${size}px`,
        borderWidth,
      }}
    />
  );
};

export default DonutSkeleton;
