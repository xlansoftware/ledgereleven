"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { Card, CardContent } from "@/components/ui/card";
import { type WidgetParams } from "@/types/widget";
import { WidgetComponent } from "@/components/widgets/WidgetComponent";

export default function EditWidgetScreen() {
  const [widgetId, setWidgetId] = useState<number | null>(null);
  const [description, setDescription] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [widgetParams, setWidgetParams] = useState<WidgetParams | null>(null);

  const handleGenerateWidget = async () => {
    setLoading(true);
    setError(null);
    setWidgetParams(null);

    try {
      // 1. Call /define-widget
      const defineRes = await fetch("/api/chart/define-widget", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userDescription: description, id: widgetId }),
      });

      if (!defineRes.ok) throw new Error("Failed to define widget");

      const defineData = await defineRes.json();

      setWidgetId(defineData.id);

      // 2. Execute the widget
      const execRes = await fetch(`/api/chart/${defineData.id}`);

      if (!execRes.ok) throw new Error("Failed to execute widget");

      debugger;

      const execData = await execRes.json();
      const fullParams: WidgetParams = {
        ...JSON.parse(execData.params),
        data: execData.data,
      };

      setWidgetParams(fullParams);
    } catch (err: any) {
      setError(err.message || "Unknown error");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex flex-col gap-6 max-w-3xl mx-auto p-4">
      <h1 className="text-2xl font-bold">Edit Widget</h1>

      <Textarea
        placeholder="Describe what you want to see..."
        value={description}
        onChange={(e) => setDescription(e.target.value)}
        className="min-h-[100px]"
      />

      <Button onClick={handleGenerateWidget} disabled={loading}>
        {loading ? "Loading..." : "Generate Preview"}
      </Button>

      {error && <p className="text-red-500">{error}</p>}

      {widgetParams && (
        <Card>
          <CardContent className="p-4">
            <h2 className="font-semibold mb-2">Chart Preview</h2>
            <WidgetComponent params={widgetParams} />
          </CardContent>
        </Card>
      )}
    </div>
  );
}
