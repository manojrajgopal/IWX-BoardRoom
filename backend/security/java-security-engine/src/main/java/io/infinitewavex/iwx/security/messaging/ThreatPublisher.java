package io.infinitewavex.iwx.security.messaging;

import java.util.Map;

import org.springframework.amqp.core.FanoutExchange;
import org.springframework.amqp.rabbit.core.RabbitTemplate;
import org.springframework.amqp.support.converter.Jackson2JsonMessageConverter;
import org.springframework.amqp.support.converter.MessageConverter;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.stereotype.Service;

@Configuration
class RabbitConfig {
    public static final String THREAT_EXCHANGE = "iwx.security.threat.detected";

    @Bean
    public FanoutExchange threatExchange() {
        return new FanoutExchange(THREAT_EXCHANGE, true, false);
    }

    @Bean
    public MessageConverter jsonMessageConverter() {
        return new Jackson2JsonMessageConverter();
    }
}

@Service
public class ThreatPublisher {

    private final RabbitTemplate rabbitTemplate;

    @Value("${iwx.rabbit.enabled:true}")
    private boolean enabled;

    public ThreatPublisher(RabbitTemplate rabbitTemplate, MessageConverter converter) {
        this.rabbitTemplate = rabbitTemplate;
        this.rabbitTemplate.setMessageConverter(converter);
    }

    public void publishThreat(String source, String subject, String severity, String category,
                              String reason, Map<String, String> metadata) {
        if (!enabled) return;
        var payload = Map.of(
            "id", java.util.UUID.randomUUID().toString(),
            "source", source,
            "subject", subject,
            "severity", severity,
            "category", category,
            "reason", reason,
            "metadata", metadata,
            "detectedAtUtc", java.time.Instant.now().toString()
        );
        try {
            rabbitTemplate.convertAndSend(RabbitConfig.THREAT_EXCHANGE, "", payload);
        } catch (Exception ex) {
            // best-effort
        }
    }
}
