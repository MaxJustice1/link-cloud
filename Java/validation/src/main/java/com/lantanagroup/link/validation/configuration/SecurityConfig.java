package com.lantanagroup.link.validation.configuration;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.security.config.Customizer;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.http.SessionCreationPolicy;
import org.springframework.security.web.SecurityFilterChain;

@Configuration
public class SecurityConfig {
    @Bean
    public SecurityFilterChain securityFilterChain(HttpSecurity http) throws Exception {

        http
            .authorizeHttpRequests(authorizeRequests -> {
                //TODO: Add more specific authorization rules here
                // - create security folder in src/main/java/com/lantanagroup/link/validation
                // - create a new Authorization Manager classes for specific authorization rules in the security folder
                // - consider putting the Authorization Manager classes in the shared module
                // - Ex: authorizeRequests.requestMatchers("/endpoint").access(customAuthorizationManager());
                // - Ex: authorizeRequests.requestMatchers("/endpoint").hasRole("ROLE_USER");
                authorizeRequests.anyRequest().authenticated();
            })
            .oauth2ResourceServer(resourceServer -> {
                resourceServer.jwt(Customizer.withDefaults());
            })
            .sessionManagement(sessionManagement -> {
                sessionManagement.sessionCreationPolicy(SessionCreationPolicy.STATELESS);
            });

       return http.build();
    }
}
