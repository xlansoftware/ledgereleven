import { HTMLAttributes, ReactNode, useRef } from "react";
import { useSpring, animated } from "@react-spring/web";
import { useDrag } from "@use-gesture/react";
import { Button } from "@/components/ui/button"; // shadcn button

interface SwipeAction {
  icon: ReactNode;
  backgroundColor: string;
  id: string;
}

interface SwipeRowProps {
  leftActions?: SwipeAction[];
  rightActions?: SwipeAction[];
  children: ReactNode;
  onAction: (actionId: string) => void;
}

const MAX_SWIPE = 100; // Max swipe distance per side

const AnimatedDiv = animated.div as React.ForwardRefExoticComponent<
  HTMLAttributes<HTMLDivElement> & React.RefAttributes<HTMLDivElement>
>;

export const SwipeRow: React.FC<SwipeRowProps> = ({
  leftActions = [],
  rightActions = [],
  children,
  onAction,
}) => {
  const rowRef = useRef<HTMLDivElement>(null);
  const [{ x }, api] = useSpring(() => ({ x: 0 }));

  const bind = useDrag(
    ({ down, movement: [mx], velocity, direction: [dx], canceled }) => {
        
      // Prevent overswipe
      if (mx > 0 && !leftActions.length) mx = 0;
      if (mx < 0 && !rightActions.length) mx = 0;
      if (Math.abs(mx) > MAX_SWIPE) mx = Math.sign(mx) * MAX_SWIPE;

      if (!down && !canceled) {
        // Snap based on swipe direction
        const threshold = 0.3;
        if (Math.abs(velocity[0]) > threshold) {
          const dir = dx > 0 ? "right" : "left";
          const hasActions =
            dir === "right" ? leftActions.length > 0 : rightActions.length > 0;

          if (hasActions) {
            api.start({ x: Math.sign(mx) * MAX_SWIPE });
            return;
          }
        }

        api.start({ x: 0 });
      } else {
        api.start({ x: down ? mx : 0, immediate: down });
      }
    },
    { axis: "x", pointer: { touch: true } }
  );

  const handleActionClick = (id: string) => {
    onAction(id);
    api.start({ x: 0 });
  };

  return (
    <div className="relative overflow-hidden touch-pan-x select-none">
      {/* Left Actions */}
      {leftActions.length > 0 && (
        <div className="absolute left-0 top-0 bottom-0 flex z-0">
          {leftActions.map((action) => (
            <div
              key={action.id}
              style={{
                backgroundColor: action.backgroundColor,
                width: MAX_SWIPE / leftActions.length,
              }}
              className="flex items-center justify-center"
            >
              <Button
                variant="ghost"
                size="icon"
                onClick={() => handleActionClick(action.id)}
                className="text-white"
              >
                {action.icon}
              </Button>
            </div>
          ))}
        </div>
      )}

      {/* Right Actions */}
      {rightActions.length > 0 && (
        <div className="absolute right-0 top-0 bottom-0 flex z-0">
          {rightActions.map((action) => (
            <div
              key={action.id}
              style={{
                backgroundColor: action.backgroundColor,
                width: MAX_SWIPE / rightActions.length,
              }}
              className="flex items-center justify-center"
            >
              <Button
                variant="ghost"
                size="icon"
                onClick={() => handleActionClick(action.id)}
                className="text-white"
              >
                {action.icon}
              </Button>
            </div>
          ))}
        </div>
      )}

      {/* Row Content */}
      <AnimatedDiv
        {...bind()}
        style={{ x } as unknown as React.CSSProperties}
        className="relative z-10 bg-white touch-pan-x"
        ref={rowRef}
      >
        {children}
      </AnimatedDiv>
    </div>
  );
};
