import { request, expect } from "playwright/test";

export const APP_URL = process.env.APP_URL || "http://localhost:5139/";
export const AUTH_URL = process.env.AUTH_URL || "http://localhost:5001/";

export async function assertTestMode() {
    const apiContext = await request.newContext()
    const response = await apiContext.get(`${AUTH_URL}api/test/mode`)
    if (!response.ok()) {
        throw new Error(`Expected the AUTH site to be in test mode: Use 'EnableEmailTestMode' appconfig.json flag`);
    }
    const mode = await response.json()
    expect(mode).toBe(true);
}

export async function createTestUser(userName: string, password: string) {
    const apiContext = await request.newContext()
    const response = await apiContext.post(`${AUTH_URL}api/test/create-user`, {
        form: {
            userName, password
        }
    })
    if (!response.ok()) {
        var text = await response.text();
        console.log(text);
        throw new Error(text);
    }
    expect(response.ok()).toBe(true);
}