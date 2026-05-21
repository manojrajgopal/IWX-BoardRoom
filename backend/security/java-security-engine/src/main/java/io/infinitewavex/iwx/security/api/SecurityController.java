package io.infinitewavex.iwx.security.api;

import java.util.List;
import java.util.Map;

import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RestController;

import io.infinitewavex.iwx.security.detection.BehavioralAnalyzer;
import io.infinitewavex.iwx.security.detection.PromptInjectionDetector;
import io.infinitewavex.iwx.security.messaging.ThreatPublisher;

@RestController
public class SecurityController {

    private final PromptInjectionDetector detector;
    private final BehavioralAnalyzer analyzer;
    private final ThreatPublisher publisher;

    public SecurityController(PromptInjectionDetector detector,
                              BehavioralAnalyzer analyzer,
                              ThreatPublisher publisher) {
        this.detector = detector;
        this.analyzer = analyzer;
        this.publisher = publisher;
    }

    @GetMapping("/health")
    public Map<String, String> health() {
        return Map.of("status", "ok", "service", "java-security-engine");
    }

    @GetMapping("/service")
    public Map<String, Object> service() {
        return Map.of(
            "key", "java-security-engine",
            "displayName", "Java Security Engine",
            "stack", "Spring Boot 3 / Java 21",
            "httpPort", 8400,
            "capabilities", List.of("prompt-injection", "behavioral-analysis", "rbac-pep")
        );
    }

    public record ScanRequest(String subject, String source, String text) {}

    @PostMapping("/scan/prompt")
    public PromptInjectionDetector.DetectionResult scanPrompt(@RequestBody ScanRequest req) {
        var result = detector.analyze(req.subject(), req.source(), req.text());
        if (result.score() >= 25) {
            publisher.publishThreat(
                result.source(), result.subject(), result.severity(),
                result.category(), String.join("; ", result.reasons()), result.metadata()
            );
        }
        return result;
    }

    public record BehaviorRequest(String subject, String action) {}

    @PostMapping("/scan/behavior")
    public BehavioralAnalyzer.AnomalyResult scanBehavior(@RequestBody BehaviorRequest req) {
        var result = analyzer.observe(req.subject(), req.action());
        if (result.score() >= 30) {
            publisher.publishThreat(
                "behavioral", result.subject(), result.severity(), "behavior",
                result.reason(),
                Map.of(
                    "action", result.action(),
                    "countInWindow", String.valueOf(result.countInWindow()),
                    "windowSeconds", String.valueOf(result.windowSeconds())
                )
            );
        }
        return result;
    }
}
