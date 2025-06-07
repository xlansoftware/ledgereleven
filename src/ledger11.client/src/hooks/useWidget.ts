import { useEffect, useState } from "react";
import { fetchWithAuth } from "@/api";
import { WidgetParams } from "@/types/widget";

export default function useWidget(id: number) {
  const [widget, setWidget] = useState<WidgetParams | null>(null);
  const [loading, setLoading] = useState(true);

  const fetchWidget = async () => {
    const response = await fetchWithAuth(`/api/chart/${id}`);
    if (response.ok) {
      const data = await response.json();

      const widgetParams: WidgetParams = {
        ...JSON.parse(data.params),
        data: data.data,
      };
      setWidget(widgetParams);
    }
    setLoading(false);
  };

  useEffect(() => {
    fetchWidget();
  }, [id]);

  return {
    widgetParams: widget,
    loading,
  };
}