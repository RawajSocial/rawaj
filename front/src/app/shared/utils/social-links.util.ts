/** Builds a link to a live Facebook post from its Graph API post id (format "{pageId}_{postId}").
 *  facebook.com/{postId} reliably redirects to the real permalink for this id shape. Instagram's
 *  publish API returns a numeric media id, not the shortcode a real instagram.com/p/ link needs,
 *  so there is deliberately no Instagram equivalent here yet. */
export function facebookPostUrl(postId: string): string {
  return `https://www.facebook.com/${postId}`;
}
