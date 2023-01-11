output "queue_env_vars" {
  value = ({
    "Components__QueueStorage" = "ArmoniK.Adapters.Amqp.QueueStorage"
    "Amqp__User"               = "${var.queue_envs.user}"
    "Amqp__Password"           = "${var.queue_envs.password}"
    "Amqp__Host"               = "${var.queue_envs.host}"
    "Amqp__Port"               = "${var.queue_envs.port}"
    "Amqp__Scheme"             = "AMQP"
    "Amqp__MaxPriority"        = "${var.queue_envs.max_priority}"
    "Amqp__MaxRetries"         = "${var.queue_envs.max_retries}"
    "Amqp__LinkCredit"         = "${var.queue_envs.link_credit}"
  })
}