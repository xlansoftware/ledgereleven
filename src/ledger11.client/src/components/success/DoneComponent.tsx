import { useEffect } from "react"
import { createPortal } from "react-dom"
import { motion } from "framer-motion"

export function DoneComponent({ onComplete }: { onComplete?: () => void }) {
  useEffect(() => {
    const timeout = setTimeout(() => {
      onComplete?.()
    }, 2000)

    return () => clearTimeout(timeout)
  }, [onComplete])

  return createPortal(
    <motion.div
      className="fixed inset-0 z-50 bg-black/50 flex items-center justify-center"
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
    >
      <motion.div
        className="bg-white rounded-full p-6 shadow-lg flex items-center justify-center"
        initial={{ scale: 0 }}
        animate={{ scale: 1 }}
        exit={{ scale: 0 }}
        transition={{ type: "spring", stiffness: 300, damping: 20 }}
      >
        <motion.svg
          xmlns="http://www.w3.org/2000/svg"
          className="h-24 w-24 text-green-500"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          initial={{ pathLength: 0 }}
          animate={{ pathLength: 1 }}
          transition={{ duration: 0.8 }}
        >
          <path d="M5 13l4 4L19 7" />
        </motion.svg>
      </motion.div>
    </motion.div>,
    document.body
  )
}
