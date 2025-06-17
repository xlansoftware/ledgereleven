import { parse } from 'node-html-parser'; // Lightweight HTML parser for Node.js

export function extractLinks(htmlText: string): string[]
{
    const root = parse(htmlText);
    const links = root.querySelectorAll('a').map(a => a.getAttribute('href'));
    return links.filter((x) => x) as string[];
}