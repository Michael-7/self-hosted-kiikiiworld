import Markdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import styles from './Post.module.css';
import type { Post as PostModel } from '../../types/post';

function formatDate(inputDate: string): string {
  return new Date(inputDate).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
}

export function Post({ post }: { post: PostModel }) {
  return (
    <div className={styles.post}>
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
