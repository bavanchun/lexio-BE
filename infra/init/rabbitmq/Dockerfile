FROM rabbitmq:3.13-management-alpine
# Enable the delayed-message-exchange community plugin for scheduled/retry messaging.
# Must be done before container startup; plugin files are baked into the image.
RUN rabbitmq-plugins enable --offline rabbitmq_delayed_message_exchange
COPY --chown=rabbitmq:rabbitmq enabled_plugins /etc/rabbitmq/enabled_plugins
USER rabbitmq
