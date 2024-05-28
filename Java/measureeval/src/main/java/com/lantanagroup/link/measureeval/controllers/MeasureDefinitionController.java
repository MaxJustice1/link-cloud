package com.lantanagroup.link.measureeval.controllers;

import com.fasterxml.jackson.annotation.JsonView;
import com.lantanagroup.link.shared.auth.PrincipalUser;
import com.lantanagroup.link.measureeval.entities.MeasureDefinition;
import com.lantanagroup.link.measureeval.repositories.MeasureDefinitionRepository;
import com.lantanagroup.link.measureeval.serdes.Views;
import com.lantanagroup.link.measureeval.services.MeasureDefinitionBundleValidator;
import com.lantanagroup.link.measureeval.services.MeasureEvaluator;
import com.lantanagroup.link.measureeval.services.MeasureEvaluatorCache;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.security.SecurityRequirement;
import io.swagger.v3.oas.annotations.Operation;
import org.hl7.fhir.r4.model.Bundle;
import org.hl7.fhir.r4.model.MeasureReport;
import org.hl7.fhir.r4.model.Parameters;
import org.springframework.http.HttpStatus;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.web.bind.annotation.*;
import org.springframework.web.server.ResponseStatusException;

import java.util.List;

@RestController
@RequestMapping("/api/measure-definition")
@PreAuthorize("hasRole('LinkUser')")
public class MeasureDefinitionController {
    private final MeasureDefinitionRepository repository;
    private final MeasureDefinitionBundleValidator bundleValidator;
    private final MeasureEvaluatorCache evaluatorCache;

    public MeasureDefinitionController(
            MeasureDefinitionRepository repository,
            MeasureDefinitionBundleValidator bundleValidator,
            MeasureEvaluatorCache evaluatorCache) {
        this.repository = repository;
        this.bundleValidator = bundleValidator;
        this.evaluatorCache = evaluatorCache;
    }

    @GetMapping
    @JsonView(Views.Summary.class)
    @Operation(summary = "Get all measure definitions", tags = {"Measure Definitions"})
    public List<MeasureDefinition> getAll(@AuthenticationPrincipal UserDetails user) {
        return repository.findAll();
    }

    @GetMapping("/{id}")
    @Operation(summary = "Get a measure definition", tags = {"Measure Definitions"})
    public MeasureDefinition getOne(@AuthenticationPrincipal PrincipalUser user, @PathVariable String id) {
        return repository.findById(id).orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND));
    }

    @PutMapping("/{id}")
    @PreAuthorize("hasAuthority('IsLinkAdmin')")
    @Operation(summary = "Put (create or update) a measure definition", tags = {"Measure Definitions"})
    public MeasureDefinition put(@AuthenticationPrincipal PrincipalUser user, @PathVariable String id, @RequestBody Bundle bundle) {
        bundleValidator.validate(bundle);
        MeasureDefinition entity = repository.findById(id).orElseGet(() -> {
            MeasureDefinition _entity = new MeasureDefinition();
            _entity.setId(id);
            return _entity;
        });
        entity.setBundle(bundle);
        repository.save(entity);
        evaluatorCache.remove(id);
        return entity;
    }

    @PostMapping("/{id}/$evaluate")
    @PreAuthorize("hasAuthority('IsLinkAdmin')")
    @Operation(summary = "Evaluate a measure against data in request body", tags = {"Measure Definitions"})
    public MeasureReport evaluate(@PathVariable String id, @RequestBody Parameters parameters) {
        MeasureEvaluator evaluator = evaluatorCache.get(id);
        if (evaluator == null) {
            throw new ResponseStatusException(HttpStatus.NOT_FOUND);
        }
        try {
            return evaluator.evaluate(parameters);
        } catch (Exception e) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, e.getMessage(), e);
        }
    }
}
