"use client";

import { useState, useEffect, lazy, Suspense } from "react";
import { Routes, Route, useLocation } from "react-router-dom";
import Header from "@/components/header";
import { Toaster } from "./ui/sonner";
import AddScreen from "./screens/screen-add";
import SpacesScreen from "./screens/screen-spaces";
import DonutSkeleton from "./DonutSkeleton";
import ResponsiveScreens from "@/components/responsive/ResponsiveScreens";
import { useMediaQuery } from "@/hooks/use-media-query";

// Use React.lazy to dynamically import the screen components:
const ScanScreen = lazy(() => import("@/components/screens/screen-scan"));
const HistoryScreen = lazy(() => import("@/components/screens/screen-history"));
const ScreenInsights = lazy(
  () => import("@/components/screens/screen-insights")
);
const Settings = lazy(() => import("@/components/screens/screen-settings"));
const CategoriesScreen = lazy(
  () => import("@/components/screens/screen-categories")
);
const EditWidgetScreen = lazy(
  () => import("@/components/screens/screen-edit-widget")
);

export default function AppLayout() {
  const location = useLocation();
  const [currentPath, setCurrentPath] = useState(location.pathname);

  const singleScreen = useMediaQuery("(max-width: 640px)");
  const doubleScreen = useMediaQuery("(max-width: 920px)");

  const screens = singleScreen
    ? [<AddScreen />]
    : doubleScreen
    ? [<AddScreen />, <HistoryScreen />]
    : [<AddScreen />, <HistoryScreen />, <ScreenInsights />];

  useEffect(() => {
    setCurrentPath(location.pathname);
  }, [location]);

  const mainScreen = <ResponsiveScreens screens={screens} />;

  return (
    <div className="flex flex-col bg-background h-full">
      {currentPath === "/login" ||
      currentPath === "/identity/account/Login" ? null : (
        <Header currentPath={currentPath} />
      )}

      <main className="flex-1 container mx-auto py-4 h-full overflow-hidden">
        <Suspense
          fallback={
            <div className="flex items-center justify-center pt-16">
              <DonutSkeleton size={220} thickness={32} />
            </div>
          }
        >
          <Routes>
            <Route path="/" element={mainScreen} />
            <Route path="/scan" element={<ScanScreen />} />
            <Route path="/history" element={<HistoryScreen />} />
            <Route path="/insights" element={<ScreenInsights />} />
            <Route path="/settings" element={<Settings />} />
            <Route path="/categories" element={<CategoriesScreen />} />
            <Route path="/spaces" element={<SpacesScreen />} />
            <Route path="/edit-widget" element={<EditWidgetScreen />} />
          </Routes>
        </Suspense>
        <Toaster
          richColors
          toastOptions={{
            classNames: {
              title: "!text-xl !font-bold",
            },
          }}
        />
      </main>
    </div>
  );
}
