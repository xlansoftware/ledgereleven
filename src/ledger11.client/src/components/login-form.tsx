import { useRef } from "react"; // Added useRef import
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { toast } from "sonner";
import { useNavigate } from "react-router-dom";
import { login } from "@/api";
import { useSpaceStore } from "@/lib/store-space";
import { useBookStore } from "@/lib/store-book";

export function LoginForm({
  className,
  ...props
}: React.ComponentPropsWithoutRef<"div">) {
  const emailRef = useRef<HTMLInputElement>(null); // Added ref for email
  const passwordRef = useRef<HTMLInputElement>(null); // Added ref for password
  const navigate = useNavigate();

  const { loadSpaces } = useSpaceStore();
  const { openBook } = useBookStore();

  const handleLogin = async (userName: string, password: string) => {
    await login(userName, password);
    const current = await loadSpaces();
    if (current) {
      await openBook(current.id!);
    } else {
      toast.error("No space found. Please create a space first.");
    }
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault(); // Prevent default form submission
    // Get values from refs
    const userName = emailRef.current?.value || "";
    const password = passwordRef.current?.value || "";

    try {
      await handleLogin(userName, password);
      navigate("/settings");
    } catch {
      toast.error("Could not login.");
    }
  };

  return (
    <div className={cn("flex flex-col gap-6", className)} {...props}>
      <Card>
        <CardHeader>
          <CardTitle className="text-2xl">Login</CardTitle>
          <CardDescription>
            Enter your email below to login to your account
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit}>
            {" "}
            {/* Added onSubmit handler */}
            <div className="flex flex-col gap-6">
              <div className="grid gap-2">
                <Label htmlFor="email">Email</Label>
                <Input
                  id="email"
                  type="email"
                  placeholder="m@example.com"
                  aria-label="Email"
                  required
                  ref={emailRef} // Added ref
                />
              </div>
              <div className="grid gap-2">
                <div className="flex items-center">
                  <Label htmlFor="password">Password</Label>
                  <a
                    href="#"
                    className="ml-auto inline-block text-sm underline-offset-4 hover:underline"
                  >
                    Forgot your password?
                  </a>
                </div>
                <Input
                  id="password"
                  type="password"
                  aria-label="Password"
                  required
                  ref={passwordRef} // Added ref
                />
              </div>
              <Button type="submit" className="w-full" aria-label="Login">
                Login
              </Button>
            </div>
            <div className="mt-4 text-center text-sm">
              Don&apos;t have an account?{" "}
              <a href="#" className="underline underline-offset-4">
                Sign up
              </a>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
