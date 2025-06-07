import { WidgetComponent } from "./WidgetComponent";
import useWidget from "@/hooks/useWidget";

interface ExecuteWidgetComponentProps {
  id: number;
}

export default function ExecuteWidgetComponent({
  id,
}: ExecuteWidgetComponentProps) {
  const { widgetParams, loading } = useWidget(id);

  console.log("Widget params:", widgetParams);

  return (
    <>
      {loading && <div>Loading...</div>}
      {!loading && !widgetParams && <div>Widget not found</div>}
      {!loading && widgetParams && <WidgetComponent params={widgetParams} />}
    </>
  );
}