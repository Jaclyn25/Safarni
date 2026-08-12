import { ActivatedRoute } from '@angular/router';

export function applyPublicMeta(doc: Document, route: ActivatedRoute): void {
  const data = route.snapshot.data as { description?: string } | undefined;
  const description = data?.description;
  if (description) {
    setMeta(doc, 'description', description);
  }

  const canonical = `${doc.location.origin}${doc.location.pathname}`;
  let link = doc.querySelector<HTMLLinkElement>('link[rel="canonical"]');
  if (!link) {
    link = doc.createElement('link');
    link.setAttribute('rel', 'canonical');
    doc.head.appendChild(link);
  }
  link.setAttribute('href', canonical);
}

function setMeta(doc: Document, name: string, content: string): void {
  let el = doc.querySelector<HTMLMetaElement>(`meta[name="${name}"]`);
  if (!el) {
    el = doc.createElement('meta');
    el.setAttribute('name', name);
    doc.head.appendChild(el);
  }
  el.setAttribute('content', content);
}
