const __vite__mapDeps=(i,m=__vite__mapDeps,d=(m.f||(m.f=["assets/TotalByPeriodByCategoryComponent-D58HlZCC.js","assets/index-BbVTBCs9.js","assets/index-CnUFKG61.css","assets/generateCategoricalChart-CuyCy8PD.js","assets/PieChart-BpkVxRR_.js","assets/HistoryComponent-B73OTwZe.js","assets/BarChart-Uxe4lV_J.js"])))=>i.map(i=>d[i]);
import{c as o,r as s,j as a,a7 as i,a8 as l,a9 as t,aa as r}from"./index-BbVTBCs9.js";/**
 * @license lucide-react v0.488.0 - ISC
 *
 * This source code is licensed under the ISC license.
 * See the LICENSE file in the root directory of this source tree.
 */const h=[["path",{d:"M3 3v16a2 2 0 0 0 2 2h16",key:"c24i48"}],["path",{d:"M18 17V9",key:"2bz60n"}],["path",{d:"M13 17V5",key:"1frdt8"}],["path",{d:"M8 17v-3",key:"17ska0"}]],d=o("chart-column",h);/**
 * @license lucide-react v0.488.0 - ISC
 *
 * This source code is licensed under the ISC license.
 * See the LICENSE file in the root directory of this source tree.
 */const m=[["path",{d:"M21 12c.552 0 1.005-.449.95-.998a10 10 0 0 0-8.953-8.951c-.55-.055-.998.398-.998.95v8a1 1 0 0 0 1 1z",key:"pzmjnu"}],["path",{d:"M21.21 15.89A10 10 0 1 1 8 2.83",key:"k2fpak"}]],p=o("chart-pie",m),u=s.lazy(()=>r(()=>import("./TotalByPeriodByCategoryComponent-D58HlZCC.js"),__vite__mapDeps([0,1,2,3,4]))),x=s.lazy(()=>r(()=>import("./HistoryComponent-B73OTwZe.js"),__vite__mapDeps([5,1,2,3,6])));function j(){const[e,n]=s.useState("total");return a.jsxs(a.Fragment,{children:[a.jsx("div",{className:"container flex items-center justify-center w-full",children:a.jsx(i,{value:e,className:"pr-4",onValueChange:c=>n(c),children:a.jsxs(l,{children:[a.jsx(t,{value:"total",className:"m-4",children:a.jsx(p,{className:"w-6 h-6"})}),a.jsx(t,{value:"history",className:"m-4",children:a.jsx(d,{className:"w-6 h-6"})})]})})}),a.jsxs(s.Suspense,{children:[e==="history"&&a.jsx(x,{}),e==="total"&&a.jsx(u,{})]})]})}export{j as default};
