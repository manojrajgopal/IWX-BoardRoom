package io.infinitewavex.iwx.security.detection;

import java.time.Instant;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import java.util.regex.Pattern;

import org.springframework.stereotype.Service;

/**
 * Heuristic prompt-injection & jailbreak detector.
 * Multi-signal scoring: pattern match, role hijack, code-fence smuggling,
 * data-exfil indicators, and suspicious URL count. Severity scales with score.
 */
@Service
public class PromptInjectionDetector {

    private static final List<Pattern> INJECTION_PATTERNS = List.of(
        Pattern.compile("(?i)ignore\\s+(all\\s+)?(previous|prior|above)\\s+(instructions|rules|prompts)"),
        Pattern.compile("(?i)disregard\\s+(the\\s+)?(system|developer|previous)\\s+(prompt|message|instructions)"),
        Pattern.compile("(?i)you\\s+are\\s+now\\s+(a|an)\\s+\\w+"),
        Pattern.compile("(?i)pretend\\s+to\\s+be"),
        Pattern.compile("(?i)act\\s+as\\s+(a|an|the)\\s+\\w+"),
        Pattern.compile("(?i)jailbreak|dan\\s+mode|developer\\s+mode\\s+enabled"),
        Pattern.compile("(?i)reveal\\s+(your|the)\\s+(system|hidden|original)\\s+prompt"),
        Pattern.compile("(?i)print\\s+(your|the)\\s+(system|hidden|initial)\\s+(prompt|instructions)"),
        Pattern.compile("(?i)\\bsystem\\s*:\\s*you\\s+are"),
        Pattern.compile("(?i)<\\|im_start\\|>|<\\|im_end\\|>|<\\|system\\|>"),
        Pattern.compile("(?i)base64\\s*decode\\s+and\\s+(run|execute)"),
        Pattern.compile("(?i)exfiltrate|exfil\\s+data|leak\\s+(the\\s+)?(api|secret|key)")
    );

    private static final List<Pattern> SENSITIVE_DATA = List.of(
        Pattern.compile("(?i)api[_-]?key\\s*[:=]"),
        Pattern.compile("(?i)password\\s*[:=]"),
        Pattern.compile("(?i)secret\\s*[:=]"),
        Pattern.compile("(?i)private[_-]?key\\s*[:=]"),
        Pattern.compile("(?i)aws[_-]?(access|secret)[_-]?key"),
        Pattern.compile("sk-[A-Za-z0-9]{20,}"),
        Pattern.compile("(?i)bearer\\s+[A-Za-z0-9._-]{20,}")
    );

    private static final Pattern URL_PATTERN = Pattern.compile("https?://[\\w.-]+");
    private static final Pattern CODE_FENCE = Pattern.compile("```[a-zA-Z]*\\s*[\\s\\S]*?```");

    public DetectionResult analyze(String subject, String source, String text) {
        if (text == null) text = "";
        int score = 0;
        List<String> hits = new ArrayList<>();

        for (var p : INJECTION_PATTERNS) {
            if (p.matcher(text).find()) {
                score += 30;
                hits.add("injection:" + p.pattern());
            }
        }
        for (var p : SENSITIVE_DATA) {
            if (p.matcher(text).find()) {
                score += 25;
                hits.add("sensitive:" + p.pattern());
            }
        }

        long urlCount = URL_PATTERN.matcher(text).results().count();
        if (urlCount >= 5) { score += 10; hits.add("many-urls:" + urlCount); }

        long fenceCount = CODE_FENCE.matcher(text).results().count();
        if (fenceCount >= 3) { score += 10; hits.add("many-code-fences:" + fenceCount); }

        if (text.length() > 8000) { score += 5; hits.add("oversized-input:" + text.length()); }

        // Cap score at 100
        score = Math.min(score, 100);

        String severity;
        if (score >= 70) severity = "Critical";
        else if (score >= 50) severity = "High";
        else if (score >= 25) severity = "Medium";
        else if (score > 0) severity = "Low";
        else severity = "Info";

        boolean blocked = score >= 50;
        String category = hits.stream().anyMatch(h -> h.startsWith("sensitive")) ? "data-exfil"
                       : hits.stream().anyMatch(h -> h.startsWith("injection")) ? "prompt-injection"
                       : "heuristic";

        Map<String, String> meta = new LinkedHashMap<>();
        meta.put("score", String.valueOf(score));
        meta.put("hitCount", String.valueOf(hits.size()));
        meta.put("urlCount", String.valueOf(urlCount));
        meta.put("length", String.valueOf(text.length()));

        return new DetectionResult(
            UUID.randomUUID(),
            source == null ? "unknown" : source,
            subject == null ? "anonymous" : subject,
            severity,
            category,
            blocked,
            score,
            hits,
            meta,
            Instant.now()
        );
    }

    public record DetectionResult(
        UUID id,
        String source,
        String subject,
        String severity,
        String category,
        boolean blocked,
        int score,
        List<String> reasons,
        Map<String, String> metadata,
        Instant detectedAtUtc
    ) {}
}
