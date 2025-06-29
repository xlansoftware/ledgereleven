import { Button } from "@/components/ui/button";

// This screen is seen only in development mode.
// Navigate to the backend at http://localhost:5139 and login
// then return back to the frontend at http://localhost:5173
export default function ScreenLogin() {
  const handleLogin = function () {
    window.location.href = `http://localhost:5139/Identity/Account/Login?ReturnUrl=${encodeURIComponent("http://localhost:5173")}`;
  };

  return (
    <div className="flex flex-col justify-center items-center px-4">
      <h1 className="text-2xl font-bold">Login</h1>
      <p className="text-muted-foreground">Please sign in to continue.</p>
      <Button className="btn mt-4" onClick={handleLogin}>Sign in</Button>
    </div>
  );
}
