import Markdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import styles from './Post.module.css';
import type { Post as PostModel } from '../../types/post';
import { API_URL } from '../../lib/api';

function formatDate(inputDate: string): string {
  return new Date(inputDate).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
}

// Legacy posts have absolute media URLs (old S3 host); ours are root-relative.
function resolveMediaUrl(url: string): string {
  return url.startsWith('/') ? `${API_URL}${url}` : url;
}

export function Post({ post }: { post: PostModel }) {
  return (
    <div className={styles.post}>
      {post.media.map((media) => (
        <img key={media.id} className={styles.image} src={resolveMediaUrl(media.url)} alt={post.title ?? ''} />
      ))}
      {post.body && (
        <div className={`${styles.content} ${styles.body}`}>
          <Markdown remarkPlugins={[remarkGfm]}>{post.body}</Markdown>
        </div>
      )}
      <div className={styles.details}>
        <span className={styles.title}>{post.title}</span>
        <span className={styles.date}>{formatDate(post.createdAt)}</span>
      </div>
    </div>
  );
}
