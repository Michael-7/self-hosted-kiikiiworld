import { useState } from 'react';
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
  const [imageIndex, setImageIndex] = useState(0);
  const media = post.media;
  const hasMultipleImages = media.length > 1;

  const showPrevImage = () => setImageIndex((index) => (index - 1 + media.length) % media.length);
  const showNextImage = () => setImageIndex((index) => (index + 1) % media.length);

  return (
    <div className={styles.post}>
      {media.length > 0 && (
        <div className={styles.gallery}>
          <img
            className={styles.image}
            src={resolveMediaUrl(media[imageIndex].url)}
            alt={post.title ?? ''}
          />
          {hasMultipleImages && (
            <>
              <button
                type="button"
                className={`${styles.caret} ${styles.caretLeft}`}
                onClick={showPrevImage}
                aria-label="Previous image"
              >
                ‹
              </button>
              <button
                type="button"
                className={`${styles.caret} ${styles.caretRight}`}
                onClick={showNextImage}
                aria-label="Next image"
              >
                ›
              </button>
            </>
          )}
        </div>
      )}
      {post.body && (
        <div className={`${styles.content} ${styles.body}`}>
          <Markdown remarkPlugins={[remarkGfm]}>{post.body}</Markdown>
        </div>
      )}
      <div className={styles.details}>
        <span className={styles.title}>{post.title}</span>
        <span className={styles.date}>
          {hasMultipleImages && (
            <span className={styles.imageCount}>
              [{imageIndex + 1}/{media.length}]{' '}
            </span>
          )}
          {formatDate(post.createdAt)}
        </span>
      </div>
    </div>
  );
}
