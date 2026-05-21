package io.infinitewavex.iwx.security.detection;

import java.time.Duration;
import java.time.Instant;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicInteger;

import org.springframework.stereotype.Service;

/**
 * Simple in-memory behavioral analyzer: tracks request rate per subject
 * with a sliding 60-second bucket. Emits anomaly score 0-100.
 */
@Service
public class BehavioralAnalyzer {

    private static final long WINDOW_SECONDS = 60;
    private static final int SUSPICIOUS_RATE = 30;
    private static final int CRITICAL_RATE = 100;

    private final Map<String, Bucket> buckets = new ConcurrentHashMap<>();

    public AnomalyResult observe(String subject, String action) {
        var now = Instant.now();
        var key = subject + "|" + action;
        var bucket = buckets.computeIfAbsent(key, k -> new Bucket(now));

        synchronized (bucket) {
            if (Duration.between(bucket.windowStart, now).getSeconds() >= WINDOW_SECONDS) {
                bucket.windowStart = now;
                bucket.count.set(0);
            }
            int count = bucket.count.incrementAndGet();
            int score = 0;
            String severity = "Info";
            String reason = "normal";
            if (count >= CRITICAL_RATE) { score = 90; severity = "Critical"; reason = "burst-rate-critical"; }
            else if (count >= SUSPICIOUS_RATE) { score = 60; severity = "High"; reason = "burst-rate-high"; }
            else if (count >= SUSPICIOUS_RATE / 2) { score = 30; severity = "Medium"; reason = "elevated-rate"; }

            return new AnomalyResult(subject, action, count, WINDOW_SECONDS, score, severity, reason);
        }
    }

    private static final class Bucket {
        Instant windowStart;
        final AtomicInteger count = new AtomicInteger(0);
        Bucket(Instant start) { this.windowStart = start; }
    }

    public record AnomalyResult(
        String subject,
        String action,
        int countInWindow,
        long windowSeconds,
        int score,
        String severity,
        String reason
    ) {}
}
